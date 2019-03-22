using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Janne252.CoH2.NewCommanders.Archiver
{
    class Program
    {
        static string ROOT;
        const string DIR_NEW_COMMANDERS = "new_commanders";
        const string DIR_GENERIC = "generic";

        static string[] DIRECTORIES;
        static string[] EXCLUDE_PATHS;

        static string AUTHOR;
        static string VERSION;

        /// <summary>
        /// Utility for exiting the app in case of an error.
        /// </summary>
        /// <param name="error"></param>
        static void Error(string error)
        {
            Console.WriteLine(error);
            Console.WriteLine("\nThe application will exit.");
            Console.ReadLine();
            Environment.Exit(1);
        }
        
        static void ValidateAndConfigure(string[] args)
        {
            if (args.Length == 0)
            {
                Error("No input provided: Please drag the new_commanders directory over the executable.");
            }
            else if (args.Length > 1)
            {
                Error($"Too many arguments: {args.Length}, expecting 1.");
            }

            ROOT = Path.GetDirectoryName(args[0]);
            if (!Directory.Exists(ROOT))
            {
                Error($"Directory \"{ROOT}\" does not exist!");
            }

            DIRECTORIES = new string[] { Path.Combine(ROOT, DIR_NEW_COMMANDERS), Path.Combine(ROOT, DIR_GENERIC) };

            if (!DIRECTORIES.All(o => Directory.Exists(o)))
            {
                Error($"One of the following paths does not exist: {String.Join(", ", DIRECTORIES.Select(o => $"\"{o}\""))}");
            }

            EXCLUDE_PATHS = new string[] {
                Path.Combine(ROOT, DIR_GENERIC, "bin"),
                Path.Combine(ROOT, DIR_GENERIC, "icons"),
                Path.Combine(ROOT, DIR_GENERIC, "scar_data"),
                Path.Combine(ROOT, DIR_GENERIC, ".git"),

                Path.Combine(ROOT, DIR_NEW_COMMANDERS, ".git"),
                Path.Combine(ROOT, DIR_NEW_COMMANDERS, "new_commanders.sga"),
                Path.Combine(ROOT, DIR_NEW_COMMANDERS, "new_commanders Intermediate Cache"),
            };
            
            Console.Write("Author name (e.g. John): ");
            AUTHOR = Console.ReadLine();
            Console.Write("Version number (e.g. 1.03): ");
            VERSION = Console.ReadLine();
        }

        static void Main(string[] args)
        {
            ValidateAndConfigure(args);
            string zipFilename = Path.Combine(ROOT, $"new-commanders-v{VERSION}-{AUTHOR}.zip");

            if (File.Exists(zipFilename))
            {
                Error($"File \"{zipFilename}\" already exists! Please try again.");
            }
            
            using (var zipFile = File.Open(zipFilename, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipOutputStream(zipFile))
            {
                foreach (var root in DIRECTORIES)
                {
                    var files = Directory
                        .GetFiles(root, "*.*", SearchOption.AllDirectories)
                        // Doesn't start with any of the exclusion root dirs
                        .Where(o => !EXCLUDE_PATHS.Any(e => o.StartsWith(e)))
                        // Is generally allowed to be included
                        .Where(o => ShouldIncludeFile(root, o))
                        .OrderBy(o => o.Length)
                        .ToList()
                    ;
                    foreach (var fileToCompress in files)
                    {
                        // Create an entry with a normalized name
                        var entry = new ZipEntry(ZipEntry.CleanName(fileToCompress.Replace(ROOT, "")));
                        zip.PutNextEntry(entry);

                        // Add the file via buffer
                        byte[] buffer = new byte[4096];
                        using (var fileStream = File.OpenRead(fileToCompress))
                        {
                            StreamUtils.Copy(fileStream, zip, buffer);
                        }

                        Console.WriteLine(fileToCompress);
                        // Close the entry
                        zip.CloseEntry();
                    }
                }
            }

            Console.WriteLine("\n\n");
            Console.WriteLine($"Successfully created {Path.GetFileName(zipFilename)}!");
            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }

        static bool ShouldIncludeFile(string root, string filename)
        {
            var extension = Path.GetExtension(filename);

            if (extension == ".psd")
            {
                return false;
            }

            return true;
        }
    }
}
