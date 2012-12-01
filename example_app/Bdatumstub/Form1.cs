using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using bdatum;

namespace Bdatumstub
{
    public partial class Form1 : Form
    {

        b_datum organization;
        b_node node;
        FileObjectList root;
        
        List<TreeItem> items = new List<TreeItem>();

        public class TreeItem
        {
            public string Name { get; set; }
            public int Level { get; set; }

            public TreeItem(string name, int level)
            {
                Name = name;
                Level = level;
            }
        }

        public Form1()
        {
            organization = new b_datum();
            organization.api_key = "";
            organization.partner_key = "";
            organization.user_name = "";
            // Always changing
            organization.organization_id = "";

            

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox3.Text = "example node";
            node = organization.node(textBox3.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            node = organization.add_node();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            

            if (node != null)
            {
                root = node.list();
                textBox1.Text = root.json;

                // for file in root.objects 
                // populate the tree view

                foreach (FileObject file in root.objects)
                {
                    items.Add(new TreeItem(file.name, 0));
                    treeView1.Nodes.Add(file.name);
                }

                //treeView1.Nodes.Clear;
                
                
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            node = organization.node_to_activate(textBox2.Text);
            textBox1.Text = node.activate();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog1.FileName;
                textBox1.Text = filename.ToString();

                node.upload(textBox2.Text, filename);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            node.download(textBox2.Text, textBox3.Text );
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox2.Text = node.info(textBox2.Text);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            node.test_json();
        }
    }
}
