using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IPTVLiveChecker
{
    /// <summary>
    /// libmpv-2.dll 最小 P/Invoke 封装（仅覆盖预览所需功能）。
    /// 关键点：mpv 的所有字符串均为 UTF-8，故全部用 byte[] 传参并手动编解码，
    /// 不能使用默认 ANSI 封送，否则中文/特殊字符（常见于 IPTV 地址）会乱码。
    /// </summary>
    internal static class MpvLib
    {
        private const string Dll = "libmpv-2.dll";

        // mpv_format 枚举（仅用到的值）
        public const int FORMAT_FLAG = 3;
        public const int FORMAT_INT64 = 4;
        public const int FORMAT_DOUBLE = 5;

        /// <summary>一旦探测到 libmpv-2.dll 缺失，永久禁用所有 P/Invoke，避免 DllNotFoundException 逸出到上层。</summary>
        public static bool Missing { get; private set; }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr mpv_create();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_initialize(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void mpv_terminate_destroy(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_set_option_string(IntPtr ctx, byte[] name, byte[] value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_set_property_string(IntPtr ctx, byte[] name, byte[] value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr mpv_get_property_string(IntPtr ctx, byte[] name);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_command(IntPtr ctx, IntPtr argv);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_get_property(IntPtr ctx, byte[] name, int format, out long data);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void mpv_free(IntPtr data);

        // ---- 安全封装：任何 P/Invoke 失败都返回安全默认，并标记 Missing ----

        public static IntPtr Create()
        {
            if (Missing) return IntPtr.Zero;
            try { return mpv_create(); }
            catch (DllNotFoundException) { Missing = true; return IntPtr.Zero; }
        }

        public static int Initialize(IntPtr ctx)
        {
            if (Missing || ctx == IntPtr.Zero) return -1;
            try { return mpv_initialize(ctx); }
            catch (DllNotFoundException) { Missing = true; return -1; }
        }

        public static void TerminateDestroy(IntPtr ctx)
        {
            if (Missing || ctx == IntPtr.Zero) return;
            try { mpv_terminate_destroy(ctx); } catch (DllNotFoundException) { Missing = true; }
        }

        public static int SetOptionString(IntPtr ctx, byte[] name, byte[] value)
        {
            if (Missing || ctx == IntPtr.Zero) return -1;
            try { return mpv_set_option_string(ctx, name, value); }
            catch (DllNotFoundException) { Missing = true; return -1; }
        }

        public static int SetPropertyString(IntPtr ctx, byte[] name, byte[] value)
        {
            if (Missing || ctx == IntPtr.Zero) return -1;
            try { return mpv_set_property_string(ctx, name, value); }
            catch (DllNotFoundException) { Missing = true; return -1; }
        }

        public static IntPtr GetPropertyString(IntPtr ctx, byte[] name)
        {
            if (Missing || ctx == IntPtr.Zero) return IntPtr.Zero;
            try { return mpv_get_property_string(ctx, name); }
            catch (DllNotFoundException) { Missing = true; return IntPtr.Zero; }
        }

        public static int Command(IntPtr ctx, IntPtr argv)
        {
            if (Missing || ctx == IntPtr.Zero) return -1;
            try { return mpv_command(ctx, argv); }
            catch (DllNotFoundException) { Missing = true; return -1; }
        }

        public static int GetProperty(IntPtr ctx, byte[] name, int format, out long data)
        {
            data = 0;
            if (Missing || ctx == IntPtr.Zero) return -1;
            try { return mpv_get_property(ctx, name, format, out data); }
            catch (DllNotFoundException) { Missing = true; return -1; }
        }

        public static void Free(IntPtr data)
        {
            if (Missing || data == IntPtr.Zero) return;
            try { mpv_free(data); } catch (DllNotFoundException) { Missing = true; }
        }

        // ---- 托管辅助 ----

        public static byte[] U8(string s) => Encoding.UTF8.GetBytes(s + "\0");

        /// <summary>把 mpv 返回的 UTF-8 指针转为托管字符串（需 mpv_free 释放）。</summary>
        public static string PtrToStringUtf8(IntPtr p)
        {
            if (p == IntPtr.Zero)
            {
                return null;
            }
            int len = 0;
            while (Marshal.ReadByte(p, len) != 0)
            {
                len++;
            }
            if (len == 0)
            {
                return string.Empty;
            }
            byte[] buf = new byte[len];
            Marshal.Copy(p, buf, 0, len);
            return Encoding.UTF8.GetString(buf);
        }

        public static string GetPropertyString(IntPtr ctx, string name)
        {
            IntPtr p = GetPropertyString(ctx, U8(name));
            if (p == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                return PtrToStringUtf8(p);
            }
            finally
            {
                Free(p);
            }
        }

        public static double? GetPropertyDouble(IntPtr ctx, string name)
        {
            string s = GetPropertyString(ctx, name);
            if (s == null)
            {
                return null;
            }
            return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) ? (double?)v : null;
        }

        /// <summary>读取 int64 类型属性（如 read-bytes / decoded-frames / displayed-frames 等统计字段）。属性不存在时返回 null。</summary>
        public static long? GetPropertyInt64(IntPtr ctx, string name)
        {
            long v = 0;
            int res = GetProperty(ctx, U8(name), FORMAT_INT64, out v);
            if (res < 0)
            {
                return null;
            }
            return v;
        }

        /// <summary>以 argv 形式下发命令（无需转义，最安全）。args 为 null 结尾由本方法处理。</summary>
        public static int Command(IntPtr ctx, params string[] args)
        {
            if (Missing || ctx == IntPtr.Zero)
            {
                return -1;
            }
            IntPtr[] native = new IntPtr[args.Length + 1];
            IntPtr[] allocated = new IntPtr[args.Length];
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(args[i] + "\0");
                    IntPtr mem = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, mem, bytes.Length);
                    native[i] = mem;
                    allocated[i] = mem;
                }
                native[args.Length] = IntPtr.Zero;
                IntPtr argv = Marshal.AllocHGlobal(IntPtr.Size * native.Length);
                Marshal.Copy(native, 0, argv, native.Length);
                int res = Command(ctx, argv);
                Marshal.FreeHGlobal(argv);
                return res;
            }
            catch (DllNotFoundException)
            {
                Missing = true;
                return -1;
            }
            finally
            {
                foreach (IntPtr p in allocated)
                {
                    if (p != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(p);
                    }
                }
            }
        }

        public static int SetProperty(IntPtr ctx, string name, string value) => SetPropertyString(ctx, U8(name), U8(value));
        public static int SetOption(IntPtr ctx, string name, string value) => SetOptionString(ctx, U8(name), U8(value));
    }
}
