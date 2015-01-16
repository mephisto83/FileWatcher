using MEPH.util.FileWatcher.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MEPH.util.FileWatcher
{
    class FileWatcherManager
    {
        IList<SubConfiguration> configurations;
        IList<SubConfiguration> Configurations
        {
            get
            {
                if (configurations == null)
                {
                    configurations = new List<SubConfiguration>();
                }

                return configurations;
            }
        }

        CommandReader reader;
        CommandReader Reader
        {
            get
            {
                if (reader == null)
                {
                    reader = new CommandReader();
                }
                return reader;
            }
        }

        private string[] args;

        public FileWatcherManager(string[] args)
        {
            // TODO: Complete member initialization
            this.args = args;

            if (this.args != null && this.args.Length > 0)
            {
                var result = Reader.LoadJson(this.args[0]);
                if (result != null)
                {
                    WriteLine("Filewatcher config: " + this.args[0]);

                    Manage(result);

                    IsReady = true;
                }
            }
        }

        private void Manage(dynamic result)
        {
            var root = result.root;
            var folderlist = new List<string>();
            filewatchers = new List<FileSystemWatcher>();

            foreach (var folder in result.folders)
            {
                string fullpath = (root + folder.name);
                folderlist.Add(fullpath);
                Configurations.Add(new SubConfiguration
                {
                    Folder = folder.name,
                    Target = folder.target,
                    Root = root
                });
            }

            foreach (var folder in folderlist)
            {
                try
                {
                    var fw = new FileSystemWatcher(folder);
                    fw.IncludeSubdirectories = true;
                    fw.Path = folder;
                    fw.EnableRaisingEvents = true;
                    fw.Changed += fw_Changed;
                    fw.Created += fw_Changed;
                    fw.Deleted += fw_Changed;
                    fw.Renamed += fw_Changed;

                    filewatchers.Add(fw);

                }
                catch (Exception e)
                {
                    WriteLine("Filewatcher : Unable to watch " + folder);
                }
            }
        }

        void fw_Changed(object sender, FileSystemEventArgs e)
        {
            WriteLine("File watcher witnessed a change.");
            var config = GetConfig(e);
            ExecuteConfiguration(config, e);
        }

        public IList<FileSystemWatcher> filewatchers;

        public bool IsReady { get; set; }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        void ExecuteConfiguration(SubConfiguration config, FileSystemEventArgs e)
        {
            if (config != null)
            {
                var subpath = e.FullPath.Substring(config.FullName.Length);
                var targetpath = config.FullTarget + subpath;
                switch (e.ChangeType)
                {
                    case System.IO.WatcherChangeTypes.Changed:
                    case System.IO.WatcherChangeTypes.Created:
                        Copy(subpath, subpath, config.FullName, config.FullTarget);
                        break;
                    case System.IO.WatcherChangeTypes.Renamed:
                        var oldpath = ((System.IO.RenamedEventArgs)(e)).OldFullPath;
                        oldpath = oldpath.Substring(config.FullName.Length);
                        Rename(oldpath, subpath, config.FullName, config.FullTarget, oldpath);
                        break;
                    case WatcherChangeTypes.Deleted:
                        Delete(subpath, subpath, config.FullName, config.FullTarget);
                        break;
                }
            }
        }

        SubConfiguration GetConfig(FileSystemEventArgs e)
        {
            var config = Configurations.FirstOrDefault(x =>
            {
                var dirinfo = new DirectoryInfo(x.Root + x.Folder);
                return e.FullPath.IndexOf(dirinfo.FullName) == 0;
            });
            return config;

        }

        void Rename(string sourceFile, string destFile, string sourcePath, string targetPath, string oldpath)
        {
            var sourcepath = System.IO.Path.Combine(sourcePath, destFile);
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(sourcepath);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                if (Directory.Exists(Path.Combine(targetPath, sourceFile)))
                {
                    Directory.Move(Path.Combine(targetPath, sourceFile), Path.Combine(targetPath, destFile));
                    // CopyOverDirectory(sourcePath, targetPath);
                }
                else
                {
                    CopyOverDirectory(sourcePath, targetPath);
                }
            }
            else
            {
                if (File.Exists(Path.Combine(targetPath, sourceFile)))
                {
                    System.IO.File.Move(Path.Combine(targetPath, sourceFile), Path.Combine(targetPath, destFile));
                    // CopyOverFile(Path.Combine(sourcePath, sourceFile), Path.Combine(targetPath, destFile), targetPath);
                }
                else
                {

                    CopyOverFile(Path.Combine(sourcePath, destFile), Path.Combine(targetPath, destFile), targetPath);
                }
            }
        }

        void Copy(string sourceFile, string destFile, string sourcePath, string targetPath)
        {
            var sourcepath = System.IO.Path.Combine(sourcePath, sourceFile);
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(sourcepath);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                CopyOverDirectory(sourcePath, targetPath);
            else
                CopyOverFile(Path.Combine(sourcePath, sourceFile), Path.Combine(targetPath, destFile), targetPath);

        }

        void Delete(string sourceFile, string destFile, string sourcePath, string targetPath)
        {
            var sourcepath = System.IO.Path.Combine(targetPath, destFile);
            // get the file attributes for file or directory
            if (File.Exists(sourcepath))
            {
                FileAttributes attr = File.GetAttributes(sourcepath);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Delete(targetPath, true);
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(targetPath, destFile)))
                    {
                        File.Delete(Path.Combine(targetPath, destFile));
                    }
                }
            }
        }

        void CopyOverFile(string sourceFile, string destFile, string targetPath)
        {
            // To copy a folder's contents to a new location:
            // Create a new target folder, if necessary.
            if (!System.IO.Directory.Exists(Path.GetDirectoryName(destFile)))
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            }

            // To copy a file to another location and 
            // overwrite the destination file if it already exists.
            System.IO.File.Copy(sourceFile, destFile, true);

        }

        void CopyOverDirectory(string sourcePath, string targetPath)
        {
            if (System.IO.Directory.Exists(sourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sourcePath);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    var fileName = System.IO.Path.GetFileName(s);
                    var destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
            else
            {
                Console.WriteLine("Source path does not exist!");
            }
        }

    }


}
