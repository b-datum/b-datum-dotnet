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

//It seems to need a higher dot net version
//using RestSharp;

namespace bdatum
{
    #region Configuration

    public static class bdatumConfigManager
    {
        public static void SaveSettings(bOrganization oconfig)
        {
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

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }

        // local path in absolute
        private Dictionary<string, bFile> filelist = new Dictionary<string,bFile>();

        private FileSystemWatcher watcher = new FileSystemWatcher();

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
                bFile newfile = new bFile(e.FullPath, node);
                filelist.Add(newfile.path, newfile);
                filelist[newfile.path].upload();
                
            }

            if (e.ChangeType.ToString() == "Deleted")
            {
                //filelist[e.FullPath].delete();
            }

            if (e.ChangeType.ToString() == "Changed")
            {
                // Class method for return just paths
                bFile newfile = new bFile(e.FullPath, node); 
                filelist[newfile.path].upload();
            }
            OnUpdated(EventArgs.Empty);
        }

        private  void OnRenamed(object source, RenamedEventArgs e)
        {
            // Now my name is different
            OnUpdated(EventArgs.Empty);            
        }

        public void readlocaldir()
        {
            string[] files = Directory.GetFiles(this.path, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                this.addfile(file);
            }

            OnUpdated(EventArgs.Empty);
        }

        public void addfile(string path)
        {
            bFile newfile = new bFile(path, node);            
            filelist.Add(newfile.path, newfile);
        }

        public List<bFile> files()
        {
            var u =  filelist.Values;
            return null;
        }         

        public void syncFileList(string path = "/")
        {
                foreach (bFile serverfile in node.list(path))
                {
                    if (!serverfile.IsDirectory())
                    {
                        serverfile.info();
                    }
                    if (filelist.ContainsKey(serverfile.path))
                    {
                        bFile localfile = filelist[serverfile.path];
   
                        if (serverfile.ETag == localfile.ETag)
                        {
                            serverfile.status = "done";
                        }
                        else
                        {
                            serverfile.status = "uploading";
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
                        serverfile.status = "done"; // Deleting
                        filelist.Add(serverfile.path, serverfile);
                        OnUpdated(EventArgs.Empty);
                    }
                }            
        }

        // Pseudo bug, always do it on c:\ ( should think about later )
        // Upload all files that are local.
        public void first_sync()
        {       
            foreach (bFile toupload in filelist.Values)
            {
                if (String.IsNullOrEmpty(toupload.local_path))
                {
                    // Should download it.
                    // Not on first sync
                }
                else
                {
                    // Add toupload to queue list
                    toupload.upload();
                }
            }
            OnUpdated(EventArgs.Empty);
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

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            return data_stream;

            //string responseFromServer = response_stream.ReadToEnd();

            //return responseFromServer;            
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

        public static long POST_PATH(string path, string auth_key)
        {  
            
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("path", path);            
            
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url + "storage");
            wr.Headers.Add("Authorization: Basic " + auth_key + "\r\n");
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            /*
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, "value", filename, "multipart/form-data");
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);            

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();
            */

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                // clean webbrowser                

                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                var content = reader2.ReadToEnd();
                var stop = "stop";

            }
            catch (Exception ex)
            {

                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            return 0;
        }
    }

    public class bOrganization
    {
        public string api_key { get; set; }
        public string partner_key { get; set; }
        public string organization_id { get; set; }
        public string user_name { get; set; }

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

        public string auth_key()
        {
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
        public string local_path { get; set; }        
        public string ETag { get; set; }
        public string filename { get; set; }
        public string type { get; set; }

        public string id { get; set; }
        public string begin_ts { get; set; }
        public string node_id { get; set; }
        public string size { get; set; }
        public string version { get; set; }
        public string mimetype_id { get; set; }
        public string mime { get; set; }
        public string path { get; set; }

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
            status = "done";
        }

        public bFile(string value) : this( value, null ) { }

        public bFile(string value, bNode node)
        {
            _node = node;

            Uri convert = new Uri(value);
            path = (convert.AbsolutePath).Substring(2);

            filename = Path.GetFileName(path);

            local_path = Path.GetFullPath(value);

            ETag = _GetMd5HashFromFile(value);

            status = "done";
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

        public string upload()
        {
            //_node.createpath(path);

            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization: Basic " + _node.auth_key());
                        
            var result = wc.UploadFile(b_http.url + "storage?path=" + path, "PUT", local_path);            

            return null;
        }

        // Todo load local_path with a likely full path ( c:\..)
        public string download(string serverpath, string savepath)
        {            
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

        public string delete(string path)
        {
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
    }

    public class bFileInfo
    {
        public string Date { get; set; }
        public string Etag { get; set; }        
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
  { "objects":
 *  {"foo": { 
 *       "versions": [
 *              {"timestamp":"2012-11-16T12:25:25.000Z","version":"1","size":"737"}
 *            ]
 *            ,"type":"file"
 *      }
 *  }
 * }
 */
