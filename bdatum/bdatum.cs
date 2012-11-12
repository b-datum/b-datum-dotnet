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

        private static string url = "https://api.b-datum.com/";

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

        public static string POST ( string path, string post_data )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "POST";

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

        public b_node add_node()
        {
            return null;
        }

        public b_node node()
        {
            return null;
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

        public string upload( string path )
        {
            return "TODO";
        }

    }

}
