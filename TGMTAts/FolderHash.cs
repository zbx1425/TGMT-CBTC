using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TGMTAts {

    public static class FolderHash {

        public static string Calculate(string dir) {
            var files = Directory.GetFiles(dir)
                .Where(f => f.EndsWith(".png") || f.EndsWith("README.txt"))
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f)
                .Select(f => Path.Combine(dir, f));
            var contents = files
                .SelectMany(f => Encoding.ASCII.GetBytes(f).Concat(File.ReadAllBytes(f)));
            var hashProvider = System.Security.Cryptography.SHA256.Create();
            var hash = hashProvider.ComputeHash(contents.ToArray());
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
