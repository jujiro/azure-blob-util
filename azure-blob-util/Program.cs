using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace abu
{
    class Program
    {
        const string STORAGE_CONNECTION_STRING = "storageConnectionString";
        const string STORAGE_CONTAINER = "storageContainer";
        const string TARGET_STORAGE_CONNECTION_STRING = "targetStorageConnectionString";
        const string TARGET_STORAGE_CONTAINER = "targetStorageContainer";
        const string MAX_THREADS = "maxThreads";
        static long _launched = 0;
        static void Main(string[] args)
        {
            Console.WriteLine(string.Format("// Blob Utility for Azure, Version={0}", Assembly.GetEntryAssembly().GetName().Version));
            if ((args.Length == 0) || (args[0] == "-?"))
            {
                ShowHelp();
                return;
            }
            string action = args[0].ToLower().TrimStart('-');
            if (!(new string[]{
                "l", "list",
                "lf", "listfile",
                "lfs", "listfilesize",
                "d", "delete",
                "g", "get",
                "gf", "getflat",
                "c", "copy"}).Contains(action))
            {
                Console.WriteLine("Invalid action. Use -? for help.");
                return;
            }

            if ((new string[] { "l", "list" }).Contains(action))
            {
                string pattern = "";
                if (args.Length > 1) pattern = args[1];
                var task = ShowList(pattern);
                task.Wait();
                return;
            }

            if ((new string[] { "lf", "listfile", "lfs", "listfilesize" }).Contains(action))
            {
                string fileName = null;
                if (args.Length > 1) fileName = args[1];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Action requires 2nd argument as file containing list of patterns.");
                    return;
                }
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File list {0} not found.", fileName);
                    return;
                }
                var showSize = ((new string[] { "lfs", "listfilesize" }).Contains(action)) ? true : false;
                var task = ShowListFile(fileName, showSize);
                task.Wait();
                return;
            }

            if ((new string[] { "g", "get", "gf", "getflat" }).Contains(action))
            {
                bool flat = false;
                if ((new string[] { "gf", "getflat" }).Contains(action)) flat = true;
                string pattern = "";
                if (args.Length > 1) pattern = args[1];
                // See if pattern is a file
                try
                {
                    if (File.Exists(pattern))
                    {
                        Console.WriteLine("Pattern {0} seems to be a file.", pattern);
                        Console.WriteLine("Blob names stored in this file will be downloaded.");
                        var t = GetBlobsFromFile(pattern, flat);
                        t.Wait();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }

                var task = Get(pattern, flat);
                task.Wait();
                return;
            }

            if ((new string[] { "d", "delete" }).Contains(action))
            {
                string fileName = null;
                if (args.Length > 1) fileName = args[1];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Delete action requires 2nd argument as file list.");
                    return;
                }
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File list {0} not found.", fileName);
                    return;
                }
                var task = DeleteBlobs(fileName);
                task.Wait();
                return;
            }

            if ((new string[] { "c", "copy" }).Contains(action))
            {
                string fileName = null;
                if (args.Length > 1) fileName = args[1];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Copy action requires 2nd argument as file list.");
                    return;
                }
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File list {0} not found.", fileName);
                    return;
                }
                var task = CopyBlobs(fileName);
                task.Wait();
                return;
            }

        }

        private async static Task ShowListFile(string fileName, bool showSize = false)
        {
            var file = new System.IO.StreamReader(fileName);
            string line = "?";
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("//")) continue;
                var task = ShowList(line, showSize);
                task.Wait();
            }
            file.Close();
        }

        private async static Task DeleteBlobs(string fileName)
        {
            Console.WriteLine("Blobs specified in {0} would be deleted", fileName);
            Console.WriteLine("Press any key to continue or ctrl-c to break");
            Console.ReadKey();
            Console.WriteLine("Starting blob deletion...");
            var timer = new Stopwatch();
            timer.Start();
            var storageConnectionString = ConfigurationManager.AppSettings[STORAGE_CONNECTION_STRING];
            var storageContainer = ConfigurationManager.AppSettings[STORAGE_CONTAINER];
            int maxThreads = Convert.ToInt32(ConfigurationManager.AppSettings[MAX_THREADS]);
            if (maxThreads == 0) maxThreads = 1;
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(storageContainer);
            var file = new System.IO.StreamReader(fileName);
            string line = "?";
            long lines = 0;
            while (line != null)
            {
                int threads = 0;
                List<Task> WaitingWork = new List<Task>();
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("//")) continue;
                    lines++;
                    var blob = blobContainer.GetBlockBlobReference(line);
                    WaitingWork.Add(blob.DeleteAsync());
                    threads++;
                    if (threads >= maxThreads) break;
                }
                try
                {
                    Task.WaitAll(WaitingWork.ToArray());
                }
                catch { }
                Console.WriteLine("{0} blobs processed for deletion in {1} ms", lines, timer.ElapsedMilliseconds);
            }
            file.Close();
            timer.Stop();
            Console.WriteLine("{0} Total blobs processed for deletion in {1} ms", lines, timer.ElapsedMilliseconds);
        }

        private async static Task ShowList(string pattern, bool showSize = false)
        {
            var storageConnectionString = ConfigurationManager.AppSettings[STORAGE_CONNECTION_STRING];
            var storageContainer = ConfigurationManager.AppSettings[STORAGE_CONTAINER];
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(storageContainer);
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            do
            {
                resultSegment = await blobContainer.ListBlobsSegmentedAsync(pattern, true, BlobListingDetails.None, null, continuationToken, null, null);
                foreach (var zz in resultSegment.Results)
                {
                    var blob = (CloudBlockBlob)zz;
                    Console.Write(blob.Name);
                    if (showSize)
                    {
                        blob.FetchAttributes();
                        Console.WriteLine(" {0}", blob.Properties.Length);
                    }
                    else
                        Console.WriteLine();
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
        }

        private async static Task GetBlobsFromFile(string fileName, bool flat)
        {
            var storageConnectionString = ConfigurationManager.AppSettings[STORAGE_CONNECTION_STRING];
            var storageContainer = ConfigurationManager.AppSettings[STORAGE_CONTAINER];
            HashSet<string> blobs = new HashSet<string>();
            using (StreamReader reader = new StreamReader(fileName))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    blobs.Add(line);
                }
            }
            bool firstTime = true;
            string downloadFolderName = null;
            int maxThreads = Convert.ToInt32(ConfigurationManager.AppSettings[MAX_THREADS]);
            if (maxThreads == 0) maxThreads = 1;
            int threads = 0;
            List<Task> WaitingWork = new List<Task>();
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(storageContainer);
            long downloadCnt = 0;
            var st = new Stopwatch();
            st.Start();
            foreach (var zz in blobs)
            {
                var blob = blobContainer.GetBlockBlobReference(zz);
                if (!blob.Exists()) continue;
                if (firstTime)
                {
                    firstTime = false;
                    downloadFolderName = Guid.NewGuid().ToString().ToLower();
                    Directory.CreateDirectory(downloadFolderName);
                }
                string blobName = flat ? blob.Name.Replace("/", "-") : Path.Combine(downloadFolderName, blob.Name);
                string blobPath = Path.GetDirectoryName(blobName);
                if (!Directory.Exists(blobPath)) Directory.CreateDirectory(blobPath);
                WaitingWork.Add(blob.DownloadToFileAsync(blobName, FileMode.Create));
                threads++;
                downloadCnt++;
                if (threads >= maxThreads)
                {
                    try
                    {
                        Task.WaitAll(WaitingWork.ToArray());
                        threads = 0;
                        WaitingWork.Clear();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                };
                if ((downloadCnt % 1000) == 0) Console.WriteLine("Blobs downloaded {0}/{1} so far in {2} ms", downloadCnt, blobs.Count, st.ElapsedMilliseconds);
            }
            Console.WriteLine("Total blobs downloaded {0}/{1} so far in {2} ms", downloadCnt, blobs.Count, st.ElapsedMilliseconds);
        }

        private async static Task Get(string pattern, bool flat)
        {
            var storageConnectionString = ConfigurationManager.AppSettings[STORAGE_CONNECTION_STRING];
            var storageContainer = ConfigurationManager.AppSettings[STORAGE_CONTAINER];
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(storageContainer);
            int maxThreads = Convert.ToInt32(ConfigurationManager.AppSettings[MAX_THREADS]);
            if (maxThreads == 0) maxThreads = 1;
            string downloadFolderName = null;
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            bool firstTime = true;
            do
            {
                resultSegment = await blobContainer.ListBlobsSegmentedAsync(pattern, true, BlobListingDetails.None, null, continuationToken, null, null);
                int threads = 0;
                List<Task> WaitingWork = new List<Task>();
                foreach (var zz in resultSegment.Results)
                {
                    var blob = (CloudBlockBlob)zz;
                    if (firstTime)
                    {
                        firstTime = false;
                        downloadFolderName = Guid.NewGuid().ToString().ToLower();
                        Directory.CreateDirectory(downloadFolderName);
                    }
                    string blobName = flat ? blob.Name.Replace("/", "-") : blob.Name;
                    WaitingWork.Add(blob.DownloadToFileAsync(blobName, FileMode.Create));
                    threads++;
                    if (threads >= maxThreads)
                    {
                        try
                        {
                            Task.WaitAll(WaitingWork.ToArray());
                        }
                        catch { }

                    };
                }
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
        }

        private async static Task CopyBlobs(string fileName)
        {
            var storageConnectionString = ConfigurationManager.AppSettings[STORAGE_CONNECTION_STRING];
            var storageContainer = ConfigurationManager.AppSettings[STORAGE_CONTAINER];
            var targetStorageConnectionString = ConfigurationManager.AppSettings[TARGET_STORAGE_CONNECTION_STRING];
            var targetStorageContainer = ConfigurationManager.AppSettings[TARGET_STORAGE_CONTAINER];
            if (string.IsNullOrWhiteSpace(targetStorageConnectionString) ||
                string.IsNullOrWhiteSpace(targetStorageContainer))
            {
                Console.WriteLine("Target connection string and container must be specified");
                return;
            }
            if ((storageConnectionString == targetStorageConnectionString) && (storageContainer == targetStorageContainer))
            {
                Console.WriteLine("Source and target must be different");
                return;
            }
            Console.WriteLine("Blobs specified in {0} would be copied from source to target", fileName);
            var timer = new Stopwatch();
            timer.Start();
            int maxThreads = Convert.ToInt32(ConfigurationManager.AppSettings[MAX_THREADS]);
            if (maxThreads == 0) maxThreads = 1;
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(storageContainer);

            var targetStorageAccount = CloudStorageAccount.Parse(targetStorageConnectionString);
            var targetBlobClient = targetStorageAccount.CreateCloudBlobClient();
            var targetBlobContainer = targetBlobClient.GetContainerReference(targetStorageContainer);

            var file = new System.IO.StreamReader(fileName);
            string line = "?";
            long lines = 0;
            var policy = new Microsoft.Azure.Storage.Blob.SharedAccessBlobPolicy()
            {
                Permissions = Microsoft.Azure.Storage.Blob.SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = new DateTimeOffset(DateTime.UtcNow.AddDays(2))
            };
            var sasToken = blobContainer.GetSharedAccessSignature(policy);
            while (line != null)
            {
                int threads = 0;
                List<Task> WaitingWork = new List<Task>();
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("//")) continue;
                    lines++;
                    var blob = blobContainer.GetBlockBlobReference(line);
                    var targetBlob = targetBlobContainer.GetBlockBlobReference(line);
                    WaitingWork.Add(targetBlob.StartCopyAsync(new Uri(blob.Uri.AbsoluteUri + sasToken)));
                    threads++;
                    if (threads >= maxThreads) break;
                }
                try
                {
                    Task.WaitAll(WaitingWork.ToArray());
                }
                catch { }
                Console.WriteLine("{0} blobs copied in {1} ms", lines, timer.ElapsedMilliseconds);
            }
            file.Close();
            timer.Stop();
            Console.WriteLine("{0} Total blobs copied in {1} ms", lines, timer.ElapsedMilliseconds);
        }
        private static void BlobCopied(IAsyncResult ar)
        {
            Interlocked.Decrement(ref _launched);
        }

        static void ShowHelp()
        {
            Console.WriteLine("Actions:");
            Console.WriteLine("  -l[ist] [pattern]");
            Console.WriteLine("  -lf or listfile <File with pattern list with one pattern per line>");
            Console.WriteLine("  -lfs or listfilesize <File with pattern list with one pattern per line>");
            Console.WriteLine("  -d[elete] <File with blob list with one blob per line>");
            Console.WriteLine("  -c[opy] <File with blob list with one blob per line>");
            Console.WriteLine("  -g[et] [pattern] Download blobs keeping directory structure");
            Console.WriteLine("  -g[et] [filename] Download blobs listed in the file keeping directory structure");
            Console.WriteLine("  -gf [pattern] Download blobs flattening the directory structure");
            Console.WriteLine("  -gf [filename] Download blobs listed in the file flattening the directory structure");
            Console.WriteLine("  -getflat [pattern] Same as -gf");
            Console.WriteLine("Files will get downloaded in a new guid folder under current working folder.");
        }
        static CloudBlobClient GetBlobClient(string storageConnectionString)
        {
            return CloudStorageAccount.Parse(storageConnectionString).CreateCloudBlobClient();
        }
    }
}
