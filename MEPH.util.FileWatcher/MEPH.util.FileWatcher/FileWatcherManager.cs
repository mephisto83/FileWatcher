using MEPH.util.FileWatcher.Data;
using Newtonsoft.Json;
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

        State ManagedState
        {
            get
            {
                if (state == null)
                    state = new State();
                return state;
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
        System.Timers.Timer timer;
        private string[] args;

        public FileWatcherManager(string[] args)
        {
            // TODO: Complete member initialization
            this.args = args;
            timer = new System.Timers.Timer(10000);
            timer.AutoReset = true;
            timer.Start();
            timer.Elapsed += timer_Elapsed;
            timer.Elapsed += WriteState;

            if (this.args != null && this.args.Length > 0)
            {
                var result = Reader.LoadJson(this.args[0]);
                if (result != null)
                {
                    WriteLine("Filewatcher config: " + this.args[0]);

                    Manage(result);

                    IsReady = true;
                    InitSystem();
                }
            }
        }

        private void InitSystem()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            var fullpath = Path.Combine(path, "filewatch.json");
            if (File.Exists(fullpath))
            {
                using (var stream = File.OpenRead(fullpath))
                {
                    var SR = new StreamReader(stream);
                    var res = SR.ReadToEnd();
                    var oldstate = JsonConvert.DeserializeObject<State>(res);
                    foreach (var state in oldstate.States)
                    {
                        if (File.Exists(state.FullName))
                        {
                            var ft = new FileInfo(state.FullName);
                            if (state.Ticks != ft.LastWriteTimeUtc.Ticks)
                            {
                                WriteLine("Update : " + state.FullName);

                                var config = GetConfig(state.FullName);

                                Backlog.Enqueue(new FileOperation
                                {
                                    Name = state.Name,
                                    Config = config,
                                    FullPath = state.FullName,
                                    Path = state.FullName,
                                    ChangeType = WatcherChangeTypes.Changed,
                                    FileState = state
                                });
                            }
                        }
                    }
                    foreach (var filepath in GetFullPaths().Where(t =>
                    {
                        var temp = oldstate.States.FirstOrDefault(x =>
                        {
                            return x.FullName == t;
                        });
                        return temp == null;
                    }))
                    {
                        var fileiinfo = new FileInfo(filepath);
                        if (File.Exists(fileiinfo.FullName))
                        {
                            var config = GetConfig(fileiinfo.FullName);
                            Backlog.Enqueue(new FileOperation
                                            {
                                                Name = fileiinfo.Name,
                                                Config = config,
                                                FullPath = fileiinfo.FullName,
                                                Path = fileiinfo.FullName,
                                                ChangeType = WatcherChangeTypes.Created,
                                                FileState = CreateState(fileiinfo)
                                            });
                        }
                    }


                }
            }
        }

        void WriteState(object sender, System.Timers.ElapsedEventArgs e)
        {
            string json = JsonConvert.SerializeObject(ManagedState);

            string path = AppDomain.CurrentDomain.BaseDirectory;
            var fullpath = Path.Combine(path, "filewatch.json");

            if (!File.Exists(fullpath))
            {
                var stream = File.Create(fullpath);

                stream.Close();
            }
            System.IO.File.WriteAllText(fullpath, json);

        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FileOperation OP = null;
            if (Backlog.Count > 0)
                OP = Backlog.Peek();
            while (OP != null)
            {

                var success = ExecuteConfiguration(OP.Config, OP);

                if (success)
                {
                    if (Backlog.Count > 0)
                        Backlog.Dequeue();
                    if (Backlog.Count > 0)
                        OP = Backlog.Peek();
                    else { OP = null; }
                }
                else
                {
                    OP = null;
                }
            }
        }

        Queue<FileOperation> Backlog = new Queue<FileOperation>();
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
            var config = GetConfig(e.FullPath);
            var file = new FileInfo(e.FullPath);
            var state = AddToState(file);
            Backlog.Enqueue(new FileOperation
            {
                Config = config,
                FullPath = e.FullPath,
                OldFullPath = ((e is System.IO.RenamedEventArgs) ? ((System.IO.RenamedEventArgs)(e)).OldFullPath : null),
                Path = e.FullPath,
                Name = e.Name,
                ChangeType = e.ChangeType,
                FileState = state
            });
        }

        private StateOfFile AddToState(FileInfo file)
        {
            var state = ManagedState.States.FirstOrDefault(x =>
            {
                return x.Ticks == file.CreationTimeUtc.Ticks && file.FullName == x.FullName;
            });

            if (state == null)
            {
                state = CreateState(file);

                ManagedState.States.Add(state);
            }

            state.LastUpdated = file.LastWriteTimeUtc.Ticks;

            return state;
        }
        StateOfFile CreateState(FileInfo file)
        {
            var state = new StateOfFile()
            {
                Name = file.Name,
                FullName = file.FullName,
                Ticks = file.CreationTimeUtc.Ticks,
                LastUpdated = file.LastWriteTimeUtc.Ticks
            };
            return state;
        }
        public IList<FileSystemWatcher> filewatchers;
        private State state;

        public bool IsReady { get; set; }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        bool ExecuteConfiguration(SubConfiguration config, FileOperation e)
        {
            var targetpath = string.Empty; ;
            try
            {
                if (config != null)
                {
                    var subpath = e.FullPath.Substring(config.FullName.Length);
                    targetpath = config.FullTarget + subpath;
                    switch (e.ChangeType)
                    {
                        case System.IO.WatcherChangeTypes.Changed:
                        case System.IO.WatcherChangeTypes.Created:
                            Copy(subpath, subpath, config.FullName, config.FullTarget);
                            break;
                        case System.IO.WatcherChangeTypes.Renamed:
                            var oldpath = e.OldFullPath;
                            oldpath = oldpath.Substring(config.FullName.Length);
                            Rename(oldpath, subpath, config.FullName, config.FullTarget, oldpath);
                            break;
                        case WatcherChangeTypes.Deleted:
                            Delete(subpath, subpath, config.FullName, config.FullTarget);
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                WriteLine("Failed to execute operation " + e.ChangeType);
                return false;
            }
            WriteLine("Successfully " + e.ChangeType + " " + targetpath);
            return true;
        }

        SubConfiguration GetConfig(string eFullPath)
        {
            var config = Configurations.FirstOrDefault(x =>
            {
                var dirinfo = new DirectoryInfo(x.Root + x.Folder);
                return eFullPath.IndexOf(dirinfo.FullName) == 0;
            });
            return config;

        }

        IList<string> GetFullPaths()
        {
            var results = new List<string>();
            foreach (var x in Configurations)
            {
                var dirinfo = new DirectoryInfo(x.Root + x.Folder);
                String[] allfiles = System.IO.Directory.GetFiles(dirinfo.FullName, "*.*", System.IO.SearchOption.AllDirectories);
                results.AddRange(allfiles);
            };
            return results;
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
