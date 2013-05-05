using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bdatum
{
    public class bRecovery
    {
        public List<bFile> ToRecover = new List<bFile>();
        #region events

        private bFileAgentNewFile bFileDetails = new bFileAgentNewFile();

        public delegate void AddedFileToListHandler(object sender, bFileAgentNewFile e); 
        public event AddedFileToListHandler AddedFileToList;
        protected virtual void OnAddedFileToList(bFileAgentNewFile e)
        {
            if (AddedFileToList != null)
                AddedFileToList(this, e);
        }

        #endregion
        #region recover

        // input directory || output directory
        public void recover()
        {
            foreach (bFile file in ToRecover)
            {
                /* NOT USED YET 
                if (!String.IsNullOrEmpty(output_directory))
                {
                    file.local_path = output_directory + file.local_path;
                }
                 */

                // check if the file is really downloaded
                // We do not recover files that are blacklisted!!!
                if (file.IsBlacklisted())
                {
                    continue;
                }              
                
                int count = 0;
                bool recovered = false;

                while (!recovered && count < 5)
                {
                    recovered = file.download();

                    bFileDetails.newfile = file;
                    OnAddedFileToList(bFileDetails);
                    count++;
                }

                if (!file.IsLocal())
                {
                    file.status = "Download Failed, tryed " + count + " times";
                    bFileDetails.newfile = file;
                    OnAddedFileToList(bFileDetails);
                }
            }
        }

        #endregion
    }

    public class bLogin
    {
        private string user;
        private string pass;        

        public bLogin(string User, string Pass)
        {
            user = User;
            pass = Pass;
        }

        public bCredential login()
        {
            string parameters = "email=" + user + "&password=" + pass;          

            try
            {
                string json = b_http.POST("login", parameters);
                bCredential credential = JsonConvert.DeserializeObject<bCredential>(json);
                return credential;
            }
            catch (Exception e)
            {
                return null;
            }            
        }
    }
    public class bCredential
    {
       public string organization_id;
       public string advanced_mode;
       public string name;
       public string phone_number;
       public string cidr;
       public string api_key;
       public string email;
       public string first_login;
       public string address;
       public string id;
       public string ts_created;

       private List<bNode> nodes = new List<bNode>();

       public List<bNode> Nodes()
       {
           try
           {
               Stream response = b_http.GET("node?api_key=" + this.api_key);
               StreamReader response_stream = new StreamReader(response);
               string responseFromServer = response_stream.ReadToEnd();

               JObject process_json = JObject.Parse(responseFromServer);

               // each one converts to json bFile
               foreach (var node in process_json["nodes"].Children())
               {              
                   bNode new_node = JsonConvert.DeserializeObject<bNode>(node.ToString());                                      
                   nodes.Add(new_node);                 
               }

               return nodes;
           }
           catch (Exception e)
           {
               return null;
           }
       }

       /* Node
        * 
        * A design mistake... I need a node object going all over...
        * 
        */
       public bNode node()
        {
            bNode node = new bNode();
            node.AuthKey = this.api_key;
            return node;
        }



    }    
}
