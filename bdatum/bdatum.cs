using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;

namespace bdatum
{

    /*
     *  Not in correct way yet, just for DRY
     */

    public class b_http
    {

        public static string url = "https://api.b-datum.com/";

        /*  TODO: Make parameters optional
         * 
         *  Make authentication optional 
         */

        public static string GET ( string path, string auth_key )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "GET";

            string authotization_header =  ("Authorization: Basic " + auth_key );
            request.Headers.Add( authotization_header );

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
    }

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

        public string list ()
        {
            return b_http.GET("storage", _auth_key() );
        }

        public string info ( string path, int version )
        {
            return "TODO";
        }

        public string download_file( string path )
        {
            return b_http.GET("storage/" + path, _auth_key());
        }

        public string download_file( string path, int version )
        {
            return b_http.GET("storage/" + path + "?version=" + version.ToString(), _auth_key());
        }
    
        public string delete( string path )
        {
            return b_http.DELETE("storage/" + path, _auth_key());
        }

        public string upload( string serverpath, string path )
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("Authorization: Basic " + _auth_key());

            wc.UploadFile( b_http.url + "/storage/" + serverpath, path);

            return null;
        }

    }
}
