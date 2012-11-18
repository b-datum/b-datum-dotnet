using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Collections.Specialized;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace bdatum
{
 
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

        public static FileObjectList load_json( string root_json )
        {
            FileObjectList root = new FileObjectList();
            root.objects = new List<FileObject>();
            root.json = root_json;

            JObject process_json = JObject.Parse(root_json);

            IList<JToken> files = process_json["objects"].Children().ToList();

            foreach (JProperty file in files)
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


    /*
     *  Not in correct way yet, just for DRY
     */

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

        public static long UPLOAD(string path, string auth_key, string file)
        {

            NameValueCollection nvc = new NameValueCollection();

            String file_hash = _GetMd5HashFromFile(file).ToUpper();          
            
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url + "/storage/"  + path );
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

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, "value", file, "multipart/form-data");
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

    /*
     *  In true this is a organization
     */

    public class b_datum
    {
        public string api_key { get; set; }
        public string partner_key { get; set; }
        public string organization_id { get; set; }
        public string user_name { get; set; }

        /*
         *  POST to /organization/{id_organizacao}/node
         * 
         *  With api_key={api_key}&name=usuario@email.com
         */

        public b_node add_node()
        {
            string answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

            // 403 error, I don´t know what to do.                        

            return null;
        }

        public b_node node_to_activate(string activation_key)
        {
            b_node node = new b_node();

            node.activation_key = activation_key;
            node.organization = this.organization_id;
            node.partner_key = this.partner_key;

            return node;
        }

        public b_node node( string node_key )
        {
            b_node node = new b_node();

            node.organization = this.organization_id;
            node.partner_key = this.partner_key;
            node.node_key = node_key;

            return node;
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

    public class b_node
    {
        public string name { get; set; }
        public string id { get; set; }
        public string organization { get; set; }
        public string partner_key { get; set; }
        public string activation_key { get; set; }
        public string node_key { get; set; }

        private string _auth_key()
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

        public FileObjectList list ()
        {
            Stream response = b_http.GET("storage", _auth_key() );            

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            FileObjectList root = FileList.load_json(responseFromServer);

            return root;
        }

        // TODO: version

        public string info ( string path )
        {
            return b_http.HEAD("storage/" + path, _auth_key());
        }
               
    
        public string delete( string path )
        {
            return b_http.DELETE("storage/" + path, _auth_key());
        }

        /*
         *  PUT works fine with default ms library.
         *  the POST requires that the upload has a value=argument, that is not supported
         *  by WebClient.
         */

        public string upload( string serverpath, string path )
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization: Basic " + _auth_key());            

            wc.UploadFile( b_http.url + "/storage/" + serverpath, "PUT", path);

            return null;            
        }

        public string download(string serverpath, string savepath)
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization: Basic " + _auth_key());

            wc.DownloadFile(b_http.url + "/storage/" + serverpath, savepath);

            return null;
        }

        public string test_json()
        {
            FileObjectList root = FileList.load_json("{\"objects\":{\"teste24\":{\"versions\":[{\"timestamp\":\"2012-11-17T11:26:50.000Z\",\"version\":\"1\",\"size\":\"737\"}],\"type\":\"file\"},\"foo\":{\"versions\":[{\"timestamp\":\"2012-11-16T12:25:25.000Z\",\"version\":\"1\",\"size\":\"737\"}],\"type\":\"file\"},\"UG9aeuQv4hGmZWPJfIxCmQ\":{\"versions\":[{\"timestamp\":\"2012-11-17T11:14:57.000Z\",\"version\":\"1\",\"size\":\"737\"}],\"type\":\"file\"},\"teste\":{\"type\":\"dir\"}}}");

            return root.json;
        }

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
}
