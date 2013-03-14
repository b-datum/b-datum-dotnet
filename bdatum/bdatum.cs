using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Collections;
using System.Collections.Specialized;

using System.Threading;
using System.Web;

//using SeasideResearch.LibCurlNet;

using System.Diagnostics;

using System.CodeDom;
using System.CodeDom.Compiler;

//It seems to need a higher dot net version
//using RestSharp;

namespace bdatum
{
    public class bFileAgentNewFile : EventArgs
    {
        public bFile newfile { get; set; }
    }

    class bWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).KeepAlive = true; // ?
                (request as HttpWebRequest).Timeout = System.Threading.Timeout.Infinite;
            }

            return base.GetWebRequest(address);
        }
    }

    #region Configuration

    public static class bdatumConfigManager
    {
        public static void SaveSettings(bOrganization oconfig)
        {
            // maybe it should be PerUserRoamingAndLocal ( in case of domains )
            Configuration roamingConfig =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);

            if( bconfig.AppSettings.Settings["api_key"] == null )
            {
                bconfig.AppSettings.Settings.Add("api_key", oconfig.api_key);
            }else{
                bconfig.AppSettings.Settings["api_key"].Value = oconfig.api_key;
            }

            if (bconfig.AppSettings.Settings["organization_id"] == null )
            {
                bconfig.AppSettings.Settings.Add("organization_id", oconfig.organization_id );
            }
            else
            {
                bconfig.AppSettings.Settings["organization_id"].Value = oconfig.organization_id;
            }               

            if( bconfig.AppSettings.Settings["partner_key"] == null )
            {
                bconfig.AppSettings.Settings.Add("partner_key", oconfig.partner_key );
            }
            else
            {
                bconfig.AppSettings.Settings["partner_key"].Value = oconfig.partner_key;
            }

            if (bconfig.AppSettings.Settings["user_name"] == null)
            {
                bconfig.AppSettings.Settings.Add("user_name", oconfig.user_name);
            }
            else
            {
                bconfig.AppSettings.Settings["user_name"].Value = oconfig.user_name;
            }

            if (bconfig.AppSettings.Settings["node_key"] == null)
            {
                bconfig.AppSettings.Settings.Add("node_key", oconfig.node_key);
            }
            else
            {
                bconfig.AppSettings.Settings["node_key"].Value = oconfig.node_key;
            }

            if (bconfig.AppSettings.Settings["last_successful_backup"] == null)
            {
                bconfig.AppSettings.Settings.Add("last_successful_backup", oconfig.last_successful_backup.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                bconfig.AppSettings.Settings["last_successful_backup"].Value = oconfig.last_successful_backup.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }


            bconfig.Save();
            //config.Save(ConfigurationSaveMode.Modified);

            string sectionName = "appSettings";
            ConfigurationManager.RefreshSection(sectionName);
        }

        public static void LoadSettings(out bOrganization oconfig)
        {
            Configuration roamingConfig =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);
            oconfig = new bOrganization();

            if (bconfig.AppSettings.Settings["api_key"] != null)
            {
                oconfig.api_key = bconfig.AppSettings.Settings["api_key"].Value;
            }
            if (bconfig.AppSettings.Settings["organization_id"] != null)
            {
                oconfig.organization_id = bconfig.AppSettings.Settings["organization_id"].Value;
            }

            if (bconfig.AppSettings.Settings["partner_key"] != null)
            {
                oconfig.partner_key = bconfig.AppSettings.Settings["partner_key"].Value;
            }

            if (bconfig.AppSettings.Settings["user_name"] != null)
            {
                oconfig.user_name = bconfig.AppSettings.Settings["user_name"].Value;
            }

            if (bconfig.AppSettings.Settings["node_key"] != null)
            {
                oconfig.node_key = bconfig.AppSettings.Settings["node_key"].Value;
            }
            if (bconfig.AppSettings.Settings["last_successful_backup"] != null)
            {
                oconfig.last_successful_backup = DateTime.Parse(bconfig.AppSettings.Settings["last_successful_backup"].Value, System.Globalization.CultureInfo.InvariantCulture); 
            }
        }

        public static void SavePath(string path)
        {
            Configuration roamingConfig =
    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);

            if (bconfig.AppSettings.Settings["backupPath"] == null)
            {
                bconfig.AppSettings.Settings.Add("backupPath", path);
            }
            else
            {
                bconfig.AppSettings.Settings["backupPath"].Value = path;
            }

            bconfig.Save();
            //config.Save(ConfigurationSaveMode.Modified);

            string sectionName = "appSettings";
            ConfigurationManager.RefreshSection(sectionName);
        }

        public static string LoadPath()
        {
            Configuration roamingConfig =
             ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);

            if (bconfig.AppSettings.Settings["backupPath"] != null)
            {
                return bconfig.AppSettings.Settings["backupPath"].Value;
            }
            else
            {
                return null;
            }
        }

        public static void SaveBlackList(List<string> blacklist)
        {
            Configuration roamingConfig =
ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);

            int index = 0;
            foreach (string path in blacklist)
            {
                string directory = "blacklist" + index.ToString();

                if (bconfig.AppSettings.Settings[directory] == null)
                {
                    bconfig.AppSettings.Settings.Add(directory, path);
                }
                else
                {
                    bconfig.AppSettings.Settings[directory].Value = path;
                }
                index++;
            }


            bconfig.Save();
            //config.Save(ConfigurationSaveMode.Modified);

            string sectionName = "appSettings";
            ConfigurationManager.RefreshSection(sectionName);
        }

        public static List<string> LoadBlackList()
        {
            Configuration roamingConfig =
 ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            ExeConfigurationFileMap bconfigFile = new ExeConfigurationFileMap();
            bconfigFile.ExeConfigFilename = roamingConfig.FilePath;

            Configuration bconfig = ConfigurationManager.OpenMappedExeConfiguration(bconfigFile, ConfigurationUserLevel.None);

            List<string> blacklist = new List<string>();

            
            for(int index = 0; index < 6; index++)
            {
                string directory = "blacklist" + index.ToString();

                if (bconfig.AppSettings.Settings[directory] != null)
                {
                    blacklist.Add( bconfig.AppSettings.Settings[directory].Value);
                }
                else
                {
                    blacklist.Add("");
                }                
            }
            return blacklist;
        }

        public static void _LoadFileSettings()
        {
#if DEBUG
            string applicationName =
                Environment.GetCommandLineArgs()[0];
#else 
           string applicationName =
          Environment.GetCommandLineArgs()[0]+ ".exe";
#endif
            string exePath = System.IO.Path.Combine(
                    Environment.CurrentDirectory, applicationName);
            System.Configuration.Configuration config =
        ConfigurationManager.OpenExeConfiguration(exePath);

        }

    }

    #endregion

    public class bFileAgent
    {
        public string path { get; set; }
        public bNode node { get; set; }
        private bool _IsRunning { get; set; }

        private bool _firstsync = false;

        public bool dummy_upload = false;
        public bool dummy_etag = false;

        // local path in absolute
        private Dictionary<string, bFile> filelist = new Dictionary<string, bFile>();

        private FileSystemWatcher watcher = new FileSystemWatcher();
        private Queue<bFile> syncronize = new Queue<bFile>();
        private bool syncronize_writing = false;

        // File Cache Info
        private string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string FileCacheInfo;
        private Dictionary<string, string> FileCache = new Dictionary<string, string>();
        public DateTime reference { get; set; }

        public bFileAgent()
        {
            FileCacheInfo = appPath + @"\FileCacheInfo.dat";
            ReadFileCacheInfo();
        }

        #region Events

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }

        public event EventHandler AfterFirstSync;
        protected virtual void OnAfterFirstSync(EventArgs e)
        {
            if (AfterFirstSync != null)
                AfterFirstSync(this, e);
        }

        public event EventHandler AfterUpdateFileList;
        protected virtual void OnAfterUpdateFileList(EventArgs e)
        {
            if (AfterUpdateFileList != null)
                AfterUpdateFileList(this, e);
        }

        public delegate void AddedFileToListHandler(object sender, bFileAgentNewFile e);   
        public event AddedFileToListHandler AddedFileToList;
        protected virtual void OnAddedFileToList(bFileAgentNewFile e)
        {
            if (AddedFileToList != null)
                AddedFileToList(this, e);
        }

        // I don´t know if it should be a eventhandler
        public event EventHandler FileSyncError;
        protected virtual void OnFileSyncError(EventArgs e)
        {
            if (FileSyncError != null)
                FileSyncError(this, e);
        }

        #endregion

        #region FileCacheInfo

        private void ReadFileCacheInfo()
        {
            if (File.Exists(FileCacheInfo))
            {
                using ( StreamReader reader = new StreamReader( FileCacheInfo) )
                {
                    while (!reader.EndOfStream)
                    {
                        string [] cacheinfo = reader.ReadLine().Split('\t');
                        if (!String.IsNullOrEmpty(cacheinfo[0]))
                        {
                            FileCache.Add( cacheinfo[1], cacheinfo[0] );
                        }                        
                    } 
                }
            }
        }

        //Destroy or at the end of first sync
        private void _WriteFileCacheInfo()
        {
            using (StreamWriter writer = new StreamWriter(FileCacheInfo))
            {
                foreach ( bFile file in filelist.Values )
                {
                    if (!String.IsNullOrEmpty(file.ETag) && !String.IsNullOrEmpty(file.local_path))
                    {
                        writer.WriteLine(file.ETag + '\t' + file.local_path);
                    }
                }
            }
         
        }

        public void FreshFileCacheInfo(DateTime Reference)
        {
            if (reference == null)
            {
                reference = Reference;
            }
            foreach( bFile file in filelist.Values )
            {
                _UpdateCachedFileETag(file);
            }

            OnUpdated(EventArgs.Empty);

            _WriteFileCacheInfo();
        }

        private void _UpdateCachedFileETag(bFile file)
        {
            if (!String.IsNullOrEmpty(file.local_path))
            {

                if (file.last_modified.CompareTo(reference) == 1)
                {
                    file.genETag();
                    file.status = "ETag cache";
                    if (FileCache.ContainsKey(file.local_path))
                    {
                        FileCache[file.local_path] = file.ETag;
                    }
                    else
                    {
                        FileCache.Add(file.local_path, file.ETag);
                    }

                }
                else
                {
                    if (String.IsNullOrEmpty(file.ETag))
                    {
                        if (FileCache.ContainsKey(file.local_path))
                        {
                            file.ETag = FileCache[file.local_path];
                        }
                        else
                        {
                            file.genETag();
                            file.status = "ETag cache";
                            FileCache.Add(file.local_path, file.ETag);
                        }
                    }
                }
            }
        }

        #endregion        

        public void AddToQueue(bFile file)
        {
            lock (this)
            {
                syncronize.Enqueue(file); 
            }
        }

        public void Syncronizer()
        {
            lock (this)
            {
                if (syncronize.Count > 0)
                {
                    bFile sync = syncronize.Dequeue();

                    switch (sync.action)
                    {
                        case "upload":
                             sync.upload();                            
                             break;
                        case "delete":
                             sync.delete("ww");
                             break;
                        default:
                            var donothing = "nod";
                            break;
                    }

                    OnUpdated(EventArgs.Empty);
                }                
            }
        }

        // Constructor?
        public void prepare()
        {
            watcher.Path = path;
            watcher.IncludeSubdirectories = true;

            // watch for changes in LastAccess and LastWrite times, 
            // and the renaming of files or directories
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Filter = "";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
        }
        // Attrib is running?
        public void run()
        {
            watcher.EnableRaisingEvents = true;
            _IsRunning = true;

            // Should run on a thread, never returns
            while (_IsRunning)
            {
                this.Syncronizer();
            }
        }

        public void stop()
        {
            watcher.EnableRaisingEvents = false;
            _IsRunning = false;
        }

        public bool IsRunning()
        {
            return _IsRunning;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Calls the call back for the interface
            if (e.ChangeType.ToString() == "Created")
            {                
                // Refactor add
                bFile file_to_queue = new bFile(e.FullPath, node);                
                filelist.Add(file_to_queue.path, file_to_queue);
                // queue ( pointer it ? )
                file_to_queue.action = "upload";
                this.AddToQueue(file_to_queue);
                //filelist[newfile.path].upload();                
            }

            if (e.ChangeType.ToString() == "Deleted")
            {
                bFile file_to_queue = filelist[e.FullPath];
                file_to_queue.action = "delete";
                this.AddToQueue( file_to_queue ); //.delete();
            }

            if (e.ChangeType.ToString() == "Changed")
            {
                // Class method for return just paths
                bFile file_to_queue = new bFile(e.FullPath, node);
                file_to_queue.action = "upload";
                this.AddToQueue(file_to_queue);
                //filelist[file_to_queue.path].upload();
            }
            // This event is trigged now by the sincronize
            //OnUpdated(EventArgs.Empty);
        }

        private  void OnRenamed(object source, RenamedEventArgs e)
        {
            // Now my name is different
            OnUpdated(EventArgs.Empty);            
        }

        public void readlocaldir()
        {
            _readlocaldir(this.path);

            OnUpdated(EventArgs.Empty);
            if ( ! _firstsync)
            {
                OnAfterUpdateFileList(EventArgs.Empty);
            }
        }

        public void _readlocaldir(string walkpath)
        {
            try
            {
                string[] files = Directory.GetFiles(walkpath, "*.*");

                foreach (string file in files)
                {
                    this.addfile(file);
                }
            }
            catch( Exception e )
            {
                var stop = e; 
            };

            try
            {
                // do something to start download
                string[] directories = Directory.GetDirectories(walkpath);
                foreach (string directory in directories)
                {
                    _readlocaldir(directory);
                }
            }
            catch (Exception e)
            {
                var stop = e;
            }

        }

        public List<bFile> files()
        {
            List<bFile> files = new List<bFile>();
            foreach (var file in filelist.Values)
            {
                files.Add(file);
            }
            return files;
        }

        public void syncFileList()
        {
            syncFileList("/");
            if (!_firstsync)
            {
                OnAfterUpdateFileList(EventArgs.Empty);
            }
        }

        public void syncFileList(string path)
        {           

                foreach (bFile serverfile in node.list(path))
                {
                    if (!serverfile.IsDirectory())
                    {
                        //serverfile.info();
                    }
                    if (filelist.ContainsKey(serverfile.path))
                    {
                        bFile localfile = filelist[serverfile.path];
   
                        if (serverfile.ETag == localfile.ETag)
                        {
                            serverfile.status = "Backup Done";
                        }
                        else
                        {
                            serverfile.status = "Uploading";
                            // ADD to upload queue
                        }
                        OnUpdated(EventArgs.Empty);
                    }
                    else
                    {
                        if (serverfile.IsDirectory())
                        {
                            syncFileList(serverfile.path);
                        }
                        serverfile.status = "Remote Only"; // Deleting ( queue )
                        filelist.Add(serverfile.path, serverfile);
                        OnUpdated(EventArgs.Empty);
                    }
                }            
        }

        // Pseudo bug, always do it on c:\ ( should think about later )
        // Upload all files that are local on first attempt
        public void full_backup()
        {

            _readlocaldir_with_upload(this.path, reference, false);            
            
//            OnUpdated(EventArgs.Empty);
            OnAfterFirstSync(EventArgs.Empty);

        }
        
        public void _readlocaldir_with_upload(string walkpath, DateTime reference, bool checkdate)
        {
            string[] files = Directory.GetFiles(walkpath, "*.*");
            foreach (string file in files)
            {

                bFile newfile = this.addfile(file);
                _UpdateCachedFileETag(newfile);
                
                //  if file is newer...
                if (reference != null && checkdate)
                {
                    if (newfile.last_modified.CompareTo(reference) > 0)
                    {  // int relative = file.last_modified.CompareTo(organization.last_successful_backup);
                        newfile.upload();
                    }else{
                        newfile.status = "not updated since last backup";
                    }
                }
                else
                {
                    newfile.upload();
                }

                // update file list
                bFileAgentNewFile e = new bFileAgentNewFile();
                e.newfile = newfile;
                OnAddedFileToList( e );
            }
            // do something to start download
            string[] directories = Directory.GetDirectories(walkpath);
            foreach (string directory in directories)
            {
                _readlocaldir_with_upload(directory,reference, checkdate);
            }
        }

        public bFile addfile(string path)
        {
            bFile newfile = new bFile(path, node);
            if (!filelist.ContainsKey(newfile.path))
            {
                filelist.Add(newfile.path, newfile);
                return newfile;
            }
            else
            {
                return filelist[newfile.path];
            }

            
        }

        public bFile file(string name)
        {
            return filelist[name];
        }
    }

    public class b_http
    {

        public static string url = "https://api.b-datum.com/";

        // wrap it!!!
        // TODO: make it in the correct way and wrap it!
        
        public static Stream GET ( string path, string auth_key )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "GET";

            string authotization_header =  ("Authorization: Basic " + auth_key );
            request.Headers.Add( authotization_header );

            try
            {
                WebResponse response = request.GetResponse();
                var status = (((HttpWebResponse)response).StatusDescription);

                Stream data_stream = response.GetResponseStream();
                StreamReader response_stream = new StreamReader(data_stream);

                return data_stream;
            }
            catch( WebException e ) 
            {
                throw e;
            }            
        }


        /*
         *  Returns a dictionaty of values on the header response
         *  Since the body is empty, it is hard to serialize now
         */ 
        public static string HEAD(string path, string auth_key)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "HEAD";

            string authotization_header = ("Authorization: Basic " + auth_key);
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;
        }

        public static string POST_HEADERS(string path, string post_data)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "POST";

            request.Headers.Add(post_data);

            byte[] byte_post_data = Encoding.UTF8.GetBytes("");

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byte_post_data.Length;

            Stream data_stream = request.GetRequestStream();
            data_stream.Write(byte_post_data, 0, byte_post_data.Length);
            data_stream.Close();

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            data_stream = response.GetResponseStream();

            StreamReader response_stream = new StreamReader(data_stream);
            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;

        }

        public static string POST ( string path, string post_data, string auth_key = null )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "POST";

            if (!String.IsNullOrEmpty(auth_key))
            {
                string authotization_header = ("Authorization: Basic " + auth_key);
                request.Headers.Add(authotization_header);
            }

            byte[] byte_post_data = Encoding.UTF8.GetBytes( post_data );

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byte_post_data.Length;

            Stream data_stream = request.GetRequestStream();
            data_stream.Write( byte_post_data, 0, byte_post_data.Length );
            data_stream.Close();

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            data_stream = response.GetResponseStream();

            StreamReader response_stream = new StreamReader( data_stream );
            string responseFromServer =  response_stream.ReadToEnd();

            return responseFromServer;

        }

        public static string DELETE(string path, string auth_key)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "DELETE";

            string authotization_header = ("Authorization: Basic " + auth_key);
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;
        }

        private static string _GetMd5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /*
         *   Hand made post, the dot net client wasn´t working ok so 
         *   I wrote it byhand to help debug and fine adjust
         *   
         *   DEPRECATED :)
         */ 
        
    }

    public class bOrganization
    {
        public string api_key { get; set; }
        public string partner_key { get; set; }
        public string organization_id { get; set; }
        public string user_name { get; set; }

        // This variables shouldn't be here, in true is 
        // time to bdatumConfigManager have a object config 
        // or not.. :P

        public DateTime last_successful_backup { get; set; }
        // Should exist a cache timestamp
        // public DateTime 

        // Here for configuration
        public string node_key { get; set; }

        // Write a decent set
        private bNode _node;       

        //Bug
        public bNode new_node()
        {
            string json_answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

            bNode new_node = JsonConvert.DeserializeObject<bNode>(json_answer);

            _node = new_node;

            return new_node;
        } 

        public bNode node()
        {
            _node = new bNode();

            _node.organization = this.organization_id;
            _node.partner_key = this.partner_key;
            _node.node_key = node_key;

            return _node;
        }

        /*
         *  TODO:
         * 
         *  The constructor of the node should be here.
         *  The node should be not directly allocated.
         *  Finish the code organization. 
         * 
         */

    }

    public class bNode
    {
        public string name { get; set; }
        public string id { get; set; }
        public string organization { get; set; }        
        public string activation_key { get; set; }

        public string partner_key { get; set; }
        public string node_key { get; set; }

        public List<string> blacklist { get; set; }

        public string auth_key()
        {
            //return "WktZcUx6SHJUb2F5WVVlY1NNUVM6SnZkaUJlOWJIZmRCNEpLRm5HOGQ=";
            string to_encode = node_key + ":" + partner_key;
            byte[] bytes_to_encode = System.Text.ASCIIEncoding.ASCII.GetBytes(to_encode);
            return System.Convert.ToBase64String(bytes_to_encode);
        }

        public string activate()
        {
            string activate_parameters = "activation_key=" + activation_key + "&partner_key=" + partner_key;
            return b_http.POST("node/activate", activate_parameters);
        }

        public List<bFile> list(string path = "/")
        {
            Stream response = b_http.GET("storage?path=" + path, auth_key());

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            JObject process_json = JObject.Parse(responseFromServer);

            //IList<JToken> files = process_json["objects"].Children();

            List<bFile> fileslist = new List<bFile>();

            // each one converts to json bFile
            foreach (var file in process_json["objects"].Children())
            {
                //string json_answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

                bFile new_file = JsonConvert.DeserializeObject<bFile>(file.ToString());
                new_file.node = this;
                fileslist.Add(new_file);
            }

            return fileslist;
        }
        
        public FileObjectList list_old ()
        {
            Stream response = b_http.GET("storage", auth_key() );            

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            FileObjectList root = FileList.load_json(responseFromServer);

            return root;
        }

        public string info ( string path )
        {
            return b_http.HEAD("storage/" + path, auth_key());
        }

        public string createpath(string path)
        {
            //string[] directories = path.Split(Path.DirectorySeparatorChar);
            string[] directories = path.Split('/');
            string build_path = "";

            foreach (string directory in directories)
            {
                build_path += directory + "/";

                WebClient wc = new WebClient();
                wc.Headers.Add("Authorization: Basic " + auth_key());
                     
                var result = wc.UploadFile(b_http.url + "storage?path=" + path, "PUT", path);
            }
            return null;
        }
    }

    public class bFile
    {

        //private string _Etag;

        private List<string> chunks = new List<string>();
        private int lastpart = 0;

        public string local_path { get; set; }        
        public string ETag { get; set; }
        public string filename { get; set; }
        public string type { get; set; }

        public long local_size { get; set; }        

        public List<string> curlcommands = new List<string>();

        // bla bla bla defines what to do
        // Dispatch table is your friend
        public string action { get; set; }

        public string id { get; set; }
        public string begin_ts { get; set; }
        public string node_id { get; set; }
        public string size { get; set; }
        public string version { get; set; }
        public string mimetype_id { get; set; }
        public string mime { get; set; }
        public string path { get; set;}

        public DateTime last_modified { get; set; }

        /* Status: 
         * processing ( first appears, local or remote )
         * local ( it is local , not in sync )
         * uploading ( it is uploading )
         * deleting ( say it )
         * done ( it is in sync )
         * 
         * remote ( only remote )
         * downloading ( only remote, downloading it valid for first sync )

         */

        public string status { get; set; }

        // reference
        private bNode _node;
        public bNode node
        {
            get { return _node; }
            set { if (_node == null) { _node = value; } }
        }

        public bFile() 
        {
            status = "";
        }

        public bFile(string value) : this( value, null ) { }

        public bFile(string value, bNode node)
        {
            _node = node;

            Uri convert = new Uri(value);
            path = (convert.AbsolutePath).Substring(2);

            filename = Path.GetFileName(path);

            local_path = Path.GetFullPath(value);

            FileInfo fileinfo = new FileInfo(local_path);
            local_size = fileinfo.Length;

            last_modified = System.IO.File.GetLastWriteTime(local_path);

            //ETag = _GetMd5HashFromFile(value);

            status = "local";
        }

        public void genETag()
        {
            ETag = _GetMd5HashFromFile(local_path);
        }

        public bool IsDirectory()
        {
            if (this.path.Substring((this.path.Length - 1)) == "/")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string _upload_external_curl()
        {
            // Verify on server on how to upload files with a espace in the name
            //string encoded_url = HttpUtility.UrlEncode(b_http.url + "storage?path=" + path);          
            
            Process curl = new Process();

                curl.StartInfo.UseShellExecute = false;
                curl.StartInfo.FileName = "curl.exe";
                curl.StartInfo.CreateNoWindow = true;
                curl.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\" -H \"Etag: " + ETag + "\" -T \"" + local_path + "\" -X PUT \"" + b_http.url + "storage?path=" + path + "\"";
                //curl.StartInfo.Arguments = " --version";
                curl.StartInfo.RedirectStandardOutput = true;
                bool started = curl.Start();

                string output = curl.StandardOutput.ReadToEnd();
                curl.WaitForExit();

                if (String.IsNullOrEmpty(output))
                {
                    status = "uploaded";
                    return output;
                }
                else
                {
                    status = "Upload Failed" + output;
                    return output;
                }
        }

        public string _upload()
        {
            bWebClient wc = new bWebClient();
            wc.Headers.Add("Authorization: Basic " + _node.auth_key());
            // Node working
            //wc.Headers.Add("Authorization: Basic WktZcUx6SHJUb2F5WVVlY1NNUVM6SnZkaUJlOWJIZmRCNEpLRm5HOGQ=");
            wc.Headers.Add("ETag: " + this.ETag);

            // Verify on server on how to upload files with a espace in the name
            //string encoded_url = HttpUtility.UrlEncode(b_http.url + "storage?path=" + path);

            var result = wc.UploadFile(b_http.url + "storage?path=" + path, "PUT", local_path);

            status = "uploaded";

            return status;
        }

        public string upload()
        {
            if (ETag == null)
            {
                genETag();
            }

            // I'm jack gigantic monolitic method
            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            // Check if file exists   POST /storage?path=/foo/bar.zip&check=1, Headers => [ Etag => 'abc123abcdef..']
            HttpWebRequest check_request = (HttpWebRequest)WebRequest.Create(b_http.url + "storage?path=" + path + "&check=1");
            check_request.Method = "POST";

            check_request.Headers.Add("Authorization: Basic " + _node.auth_key());
            check_request.Headers.Add("Etag: " + ETag);
            check_request.ContentType = "application/json";
            check_request.Accept = "application/json";
            try
            {
                var check_response = (HttpWebResponse)check_request.GetResponse();
                using (var streamReader = new StreamReader(check_response.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    //Now you have your response.
                    //or false depending on information in the response
                    if (check_response.StatusCode == HttpStatusCode.Created)
                    {
                        status = "uploaded*";
                        return responseText;
                    }
                }
            }
            catch (Exception e)
            {
                var stop = "it seems that the file doesn't exist";
            }

            
            //if (local_size > 90 * 1024 * 1024)
            if (local_size > 7 * 1024 * 1024)
            {
                splitfile();

                bParts parts = new bParts();                

                Process curlinit = new Process();
                curlinit.StartInfo.UseShellExecute = false;
                curlinit.StartInfo.FileName = "curl.exe";
                curlinit.StartInfo.CreateNoWindow = true;
                curlinit.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\"" + "\" -H \"Etag: " + ETag + "\" -X POST \"" + b_http.url + "storage?path=" + path + "&multipart=1";
                curlinit.StartInfo.RedirectStandardOutput = true;
                curlinit.Start();
                string answer = curlinit.StandardOutput.ReadToEnd();

                bMultipartInfo multipart = JsonConvert.DeserializeObject<bMultipartInfo>(answer);
                    

                // HERE
                string upload_id = multipart.upload_id;

                curlinit.WaitForExit();

                curlcommands.Add(curlinit.StartInfo.Arguments);

                int part = 1;

                foreach (string chunk in chunks)
                {
                    
                    //resume
                    if (part < lastpart)
                    {
                        part++;
                        continue;
                    }

                    string md5sum = _GetMd5HashFromFile(chunk);

                    string[] detail = new string[] { part.ToString(), md5sum };

                    parts.parts.Add( detail);

                    //continue;

                    Process curl = new Process();

                    curl.StartInfo.UseShellExecute = false;
                    curl.StartInfo.FileName = "curl.exe";
                    curl.StartInfo.CreateNoWindow = true;
                    curl.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\" -H \"Etag: " + md5sum + "\" -T \"" + chunk + "\" -X PUT \"" + b_http.url + "storage?path=" + path + "&part=" + part.ToString() + "&upload_id=" + upload_id + "\"";
                    //curl.StartInfo.Arguments = " --version";
                    curl.StartInfo.RedirectStandardOutput = true;
                    bool started = curl.Start();

                    string output = curl.StandardOutput.ReadToEnd();
                    curl.WaitForExit();

                    curlcommands.Add(curl.StartInfo.Arguments);

                    //File.Delete(chunk);
                    if (String.IsNullOrEmpty(output))
                    {
                        status = "uploaded part " + part.ToString();
                        lastpart = part;
                        //return output;
                    }
                    else
                    {
                        status = "Error: Upload part" + part.ToString() + " " + output;
                        //return output;
                    }
                    part++;
                }

                string send = JsonConvert.SerializeObject(parts);
                /*
                using (var writer = new StringWriter())
                {
                    using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                    {
                        provider.GenerateCodeFromExpression(new CodePrimitiveExpression(send), writer, null);
                        send = writer.ToString();
                    }
                }
                */
                // Last part
                
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(b_http.url + "storage?path=" + path + "&upload_id=" + upload_id );
                request.Method = "POST";
                
                request.Headers.Add("Authorization: Basic " + _node.auth_key());
                request.Headers.Add("Etag: " + ETag);
                request.ContentType = "application/json";
                request.Accept = "application/json";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(send);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    //Now you have your response.
                    //or false depending on information in the response


                    status = "almost ended" + response.Headers.ToString();
                    return responseText;
                }              
            }
            else
            {
                return _upload_external_curl();
            }

        }

        public string curl_upload()
        {
            /*
            Easy easy = new Easy();

            easy.SetOpt(CURLoption.CURLOPT_HEADER, true);
            // Slist? 
            easy.SetOpt(CURLoption.CURLOPT_HTTPHEADER, null);
            */

            return null;

        }

        // Todo load local_path with a likely full path ( c:\..)
        public string download(string serverpath, string savepath)
        {

            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization: Basic " + _node.auth_key());

            wc.DownloadFile(b_http.url + "/storage/" + path, local_path);

            return null;
        }

        // Copy and Paste, shame on me
        private string _GetMd5HashFromFile(string fullpath)
        {
            if (fullpath == null)
            {
                fullpath = local_path;
            }
            try
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var buffer = md5.ComputeHash(File.ReadAllBytes(fullpath));
                    var sb = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        sb.Append(buffer[i].ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public string delete(string path)
        {
            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            return b_http.DELETE("storage/" + path, _node.auth_key());
        }

        public void info()
        {
            WebRequest request = WebRequest.Create( b_http.url + "storage?path=" + path);
            request.Method = "HEAD";

            string authotization_header = ("Authorization: Basic " + node.auth_key());
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            
            // Dot net 2.0 arrays does not support directly contains
            List<string> headers = new List<string>(response.Headers.AllKeys);

            if (headers.Contains("ETag"))
            {
                ETag = response.Headers["ETag"];
            }
            
        }      
  
        public string UPLOADNEW()
        {  
            string auth_key = _node.auth_key();

            // ok
            //string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            string boundary = "xYzZY" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest upload = (HttpWebRequest)WebRequest.Create(b_http.url + "storage");
            upload.ContentType = "multipart/form-data; boundary=" + boundary;
            upload.Method = "POST";
            upload.Headers.Add("Authorization: Basic " + auth_key);
            upload.Headers.Add("ETag: " + this.ETag);                      

            //upload.AllowWriteStreamBuffering = false;

            Stream uploadStream = upload.GetRequestStream();

            FileStream uploadFile = new FileStream(this.local_path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new Byte[4096];
            int bytesRead = 0;
            while (( bytesRead = uploadFile.Read(buffer, 0, buffer.Length)) != 0 )
                uploadStream.Write(buffer, 0, bytesRead);
            uploadFile.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n==" + boundary + "--\r\n");
            uploadStream.Write(trailer, 0, trailer.Length);
            uploadStream.Close();

            WebResponse response = upload.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader responseStreamReader = new StreamReader(responseStream);
            String returnString = responseStreamReader.ReadToEnd();

            uploadFile.Close();
            responseStream.Close();
            responseStreamReader.Close();
            uploadStream.Close();

            return returnString;
        }

        public bool splitfile()
        {
            // I need to check is enough disk space to continue.

            DriveInfo[] alldrives = DriveInfo.GetDrives();
            
            string temp_path = System.IO.Path.GetTempPath();

            string drive_letter = temp_path.Substring(0, 3);

            foreach (var drive in alldrives)
            {
                if (drive.Name == drive_letter)
                {
                    if (drive.TotalFreeSpace < local_size )
                    {
                        this.status = "Error: Not enough disk space";
                        throw new System.OperationCanceledException("Not enoughs disk space");
                    }
                }
            }

            // Resume
            if (lastpart > 0)
            {
                return true;
            }

            const int BUFFER_SIZE = 10 * 1024;    
        
            //int chunkSize = 90 * 1024 * 1024;
            int chunkSize = 7 * 1024 * 1024;

            byte[] buffer = new byte[BUFFER_SIZE];

            using (Stream input = File.OpenRead(local_path))
            {
                int index = 0;
                while (input.Position < input.Length)
                {
                    string temp_filename = temp_path + "\\" + filename + "__" + index.ToString();
                    chunks.Add(temp_filename);
                    using (Stream output = File.Create( temp_filename ))
                    {
                        int remaining = chunkSize, bytesRead;
                        while( remaining > 0 && ( bytesRead = input.Read(buffer, 0, Math.Min(remaining, BUFFER_SIZE))) > 0 )
                        {
                            output.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }
                    }
                    index++;
                    Thread.Sleep(500); 
                }
            }

            return true;
        }

        private bool _blacklisted()
        {
            List<string> blacklist = node.blacklist;

            string mypath = Path.GetDirectoryName(local_path);

            foreach (string path in blacklist)
            {
                if (mypath == path)
                    return true;
            }
            return false;
        }
    }

    public class bFileInfo
    {
        public string Date { get; set; }
        public string Etag { get; set; }        
    }

    public class bMultipartInfo
    {
        public string upload_id { get; set; }        
    }

    public class bParts
    {
        public List<string[]> parts = new List<string[]>();
    }
    

    #region deprecated

    public class Version
    {
        // order by version ( it is possible, check on api )
        public string timestamp { get; set; }
        public string version { get; set; }
        public string size { get; set; }
    }

    public class VersionList
    {
        public List<Version> versions { get; set; }
        public string type { get; set; }
    }

    public class FileObject
    {
        public VersionList versions { get; set; }
        public string name { get; set; }
    }

    public class FileObjectList
    {
        public List<FileObject> objects { get; set; }
        public string json { get; set; }
    }

    public static class FileList
    {

        public static FileObjectList load_json(string root_json)
        {
            FileObjectList root = new FileObjectList();
            root.objects = new List<FileObject>();
            root.json = root_json;

            JObject process_json = JObject.Parse(root_json);

            //IList<JToken> files = process_json["objects"].Children();

            foreach (JProperty file in process_json["objects"].Children())
            {
                string name = file.Name;
                string json = file.Value.ToString();

                FileObject fileObject = new FileObject();
                fileObject.name = name;

                //VersionList
                VersionList fileVersions = JsonConvert.DeserializeObject<VersionList>(json);

                fileObject.versions = fileVersions;

                root.objects.Add(fileObject);
            }

            return root;

        }

    }

    #endregion
}

/*
                Process curlend = new Process();
                caurlend.StartInfo.UseShellExecute = false;
                curlend.StartInfo.FileName = "curl.exe";
                curlend.StartInfo.CreateNoWindow = true;
                //curlend.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\"" + " -H \"Content-Type: application/json\" -H \"Accept: application/json\" -H \"Etag:" + ETag + "\" -X POST -d \"" + send + "\" --url \"" + b_http.url + "storage?path=" + path + "&upload_id=" + upload_id + "\"";
                //curlend.StartInfo.Arguments = string.Format(@" -d ""parts={2}"" -k -v  -H ""Authorization: Basic {0}"" -H ""Content-Type: application/json"" -H ""Accept: application/json"" -H ""Etag: {1}"" --url ""{3}"" ", _node.auth_key(), ETag, send, "http://192.168.2.25:3025?path=/&input=432423423"); // b_http.url + "storage?path=" + path + "&upload_id=" + upload_id);
                curlend.StartInfo.Arguments = string.Format(@"-k --trace-ascii tracy -d @send.txt -H ""Authorization: Basic {0}""  -H ""Content-Type: application/json"" -H ""Accept: application/json""  -H ""Etag: {1}"" --url ""{3}"" ", _node.auth_key(), ETag, send, b_http.url + "storage?path=" + path + "&upload_id=" + upload_id);
                curlend.StartInfo.RedirectStandardOutput = true;
                curlend.StartInfo.RedirectStandardError = true;
                curlend.StartInfo.RedirectStandardInput = true;
                curlend.Start();

                string last_answer = curlend.StandardOutput.ReadToEnd();
                string last_error = curlend.StandardError.ReadToEnd();
                
                curlend.WaitForExit();                

                curlcommands.Add(curlend.StartInfo.Arguments);

                if (String.IsNullOrEmpty(last_answer))
                {
                    status = "uploaded  " + curlend.StartInfo.Arguments + "  " + last_answer; // +last_error;
                    return last_answer;
                }
                else
                {
                    status = "Error: Closing the upload" + last_answer;
                    return last_answer;
                }                
*/