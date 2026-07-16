using System;
using System.IO;
using System.Text;

namespace TextCompare.IO
{
    /// <summary>
    /// 파일 인코딩을 판별한다. BOM이 있으면 그대로 신뢰하고,
    /// 없으면 UTF-8 엄격 디코딩을 시도한 뒤 실패 시 CP949(EUC-KR)로 폴백한다.
    /// </summary>
    public static class EncodingDetector
    {
        public static Encoding Detect(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Detect(bytes);
        }

        public static Encoding Detect(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            Encoding bom = DetectByBom(bytes);
            if (bom != null) return bom;

            if (IsValidUtf8(bytes)) return new UTF8Encoding(false);

            try
            {
                return Encoding.GetEncoding(949); // CP949 / EUC-KR
            }
            catch (ArgumentException)
            {
                return Encoding.Default;
            }
        }

        public static string ReadAllText(string path, out Encoding detectedEncoding)
        {
            byte[] bytes = File.ReadAllBytes(path);
            detectedEncoding = Detect(bytes);

            int preambleLength = GetPreambleLength(bytes, detectedEncoding);
            return detectedEncoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        }

        private static int GetPreambleLength(byte[] bytes, Encoding encoding)
        {
            byte[] preamble = encoding.GetPreamble();
            if (preamble.Length == 0 || bytes.Length < preamble.Length) return 0;

            for (int i = 0; i < preamble.Length; i++)
            {
                if (bytes[i] != preamble[i]) return 0;
            }
            return preamble.Length;
        }

        private static Encoding DetectByBom(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(true);

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode; // UTF-16 LE

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode; // UTF-16 BE

            return null;
        }

        private static bool IsValidUtf8(byte[] bytes)
        {
            int i = 0;
            int n = bytes.Length;
            while (i < n)
            {
                byte b = bytes[i];

                if (b <= 0x7F) { i++; continue; }

                int extra;
                int min;
                if ((b & 0xE0) == 0xC0) { extra = 1; min = 0x80; }
                else if ((b & 0xF0) == 0xE0) { extra = 2; min = 0x800; }
                else if ((b & 0xF8) == 0xF0) { extra = 3; min = 0x10000; }
                else { return false; }

                if (i + extra >= n) return false;

                int codePoint = b & (0xFF >> (extra + 2));
                for (int j = 1; j <= extra; j++)
                {
                    byte cb = bytes[i + j];
                    if ((cb & 0xC0) != 0x80) return false;
                    codePoint = (codePoint << 6) | (cb & 0x3F);
                }

                if (codePoint < min) return false; // overlong encoding
                if (codePoint > 0x10FFFF) return false;
                if (codePoint >= 0xD800 && codePoint <= 0xDFFF) return false; // surrogate 범위

                i += extra + 1;
            }
            return true;
        }
    }
}
