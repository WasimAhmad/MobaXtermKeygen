using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MobaXtermKeygen
{
    internal class Program
    {
        private const string VariantBase64Table = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        private static readonly Dictionary<int, char> VariantBase64Dict = VariantBase64Table.Select((value, index) => new { value, index }).ToDictionary(pair => pair.index, pair => pair.value);
        private static readonly Dictionary<char, int> VariantBase64ReverseDict = VariantBase64Table.Select((value, index) => new { value, index }).ToDictionary(pair => pair.value, pair => pair.index);

        private enum LicenseType
        {
            Professional = 1,
            Educational = 3,
            Personal = 4
        }

        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                DisplayHelp();
                return;
            }

            if (!TryParseVersion(args[1], out int majorVersion, out int minorVersion))
            {
                Console.WriteLine("Invalid version format. Expected format: Major.Minor (e.g., 10.9)");
                return;
            }

            var userName = args[0];
            GenerateLicense(LicenseType.Professional, 1, userName, majorVersion, minorVersion);
            Console.WriteLine($"[*] Success!");
            Console.WriteLine($"[*] File generated: {Path.Combine(Directory.GetCurrentDirectory(), "Custom.mxtpro")}");
            Console.WriteLine($"[*] Please move or copy the newly-generated file to MobaXterm's installation path.");
        }

        public static bool TryParseVersion(string versionString, out int majorVersion, out int minorVersion)
        {
            string[] versionParts = versionString.Split('.');
            if (versionParts.Length != 2)
            {
                // Assign default values before exiting
                majorVersion = 0;
                minorVersion = 0;
                return false;
            }

            bool majorSuccess = int.TryParse(versionParts[0], out majorVersion);
            bool minorSuccess = int.TryParse(versionParts[1], out minorVersion);

            return majorSuccess && minorSuccess;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    MobaXterm-Keygen.exe <UserName> <Version>");
            Console.WriteLine();
            Console.WriteLine("    <UserName>:      The Name licensed to");
            Console.WriteLine("    <Version>:       The Version of MobaXterm");
            Console.WriteLine("                     Example:    10.9");
        }

        private static void GenerateLicense(LicenseType type, int count, string userName, int majorVersion, int minorVersion)
        {
            string licenseString = CreateLicenseString(type, count, userName, majorVersion, minorVersion);
            Console.Write(licenseString);
            var encodedLicenseString = VariantBase64Encode(EncryptBytes(0x787, Encoding.ASCII.GetBytes(licenseString)));
            SaveLicenseToZip(encodedLicenseString);
        }

        private static string CreateLicenseString(LicenseType type, int count, string userName, int majorVersion, int minorVersion)
        {
            return $"{(int)type}#{userName}|{majorVersion}{minorVersion}#{count}#{majorVersion}3{minorVersion}6{minorVersion}#0#0#0#";
        }

        private static void SaveLicenseToZip(string encodedLicense)
        {
            using var zip = new ZipArchive(new FileStream("Custom.mxtpro", FileMode.Create), ZipArchiveMode.Create);
            var entry = zip.CreateEntry("Pro.key");
            using var stream = new StreamWriter(entry.Open());
            stream.Write(encodedLicense);
        }

        static byte[] EncryptBytes(int key, byte[] data)
        {
            var result = new List<byte>();
            foreach (var b in data)
            {
                result.Add((byte)(b ^ ((key >> 8) & 0xff)));
                key = result.Last() & key | 0x482D;
                //Console.WriteLine($"Byte: {b}, Key: {key}, Result Byte: {result.Last()}");

            }
            return result.ToArray();
        }

        public static string VariantBase64Encode(byte[] data)
        {
            const string VariantBase64Table = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
            StringBuilder result = new StringBuilder();

            int blockCount = data.Length / 3;
            int remainingBytes = data.Length % 3;

            for (int i = 0; i < blockCount; i++)
            {
                byte[] threeBytes = data.Skip(3 * i).Take(3).ToArray();
                byte[] fourBytes = new byte[4] { threeBytes[0], threeBytes[1], threeBytes[2], 0 };
                int codingInt = BitConverter.ToInt32(new byte[4] { threeBytes[0], threeBytes[1], threeBytes[2], 0 }, 0);

                result.Append(VariantBase64Table[codingInt & 0x3f]);
                result.Append(VariantBase64Table[(codingInt >> 6) & 0x3f]);
                result.Append(VariantBase64Table[(codingInt >> 12) & 0x3f]);
                result.Append(VariantBase64Table[(codingInt >> 18) & 0x3f]);
            }

            switch (remainingBytes)
            {
                case 1:
                    int codingInt1 = data[data.Length - 1];
                    result.Append(VariantBase64Table[codingInt1 & 0x3f]);
                    result.Append(VariantBase64Table[(codingInt1 >> 6) & 0x3f]);
                    //result.Append("==");
                    break;

                case 2:
                    int codingInt2 = BitConverter.ToInt16(data.Skip(data.Length - 2).Take(2).ToArray(), 0);
                    result.Append(VariantBase64Table[codingInt2 & 0x3f]);
                    result.Append(VariantBase64Table[(codingInt2 >> 6) & 0x3f]);
                    result.Append(VariantBase64Table[(codingInt2 >> 12) & 0x3f]);
                    //result.Append("=");
                    break;
            }

            return result.ToString();
        }


    }
}
