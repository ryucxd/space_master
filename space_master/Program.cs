using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using System.Data.SqlClient;
using System.Data;
using System.Data.SqlClient;

namespace space_master
{
    class Program
    {
        static void Main(string[] args)
        {

            int id = 0;
            string program = "106963_05";
            

            //need to read 
            string file = @"\\designsvr1\dropbox\106963_05.NC";

            string test = File.ReadAllText(file);
            //repplace the the - with ""

            string[] arrLine = File.ReadAllLines(file);

            string sql = "";
            string x_dim = "";
            string y_dim = "";
            double highest_y = 0;

            x_dim = arrLine[6].Substring(6);
            y_dim = arrLine[7].Substring(6);

            Console.WriteLine(x_dim);
            Console.WriteLine(y_dim);

            for (int i = 1; i < arrLine.Count(); i++)
            {
                //check if the line starts with an X
                if (arrLine[i].Substring(0, 1) == "X")
                {
                    //check if it has a Y aswell
                    if (arrLine[i].Contains("Y"))
                    {
                        string y_value = arrLine[i].Substring(arrLine[i].LastIndexOf("Y") + 1);

                        int y_value_trim = y_value.Length;
                        while (y_value_trim <= y_value.Length)
                        {
                            bool isNumeric = float.TryParse(y_value, out _);
                            if (isNumeric == true)
                                y_value_trim = 999;
                            else
                            {
                                //take 1 off the length of the y_value
                                y_value_trim--;
                                y_value = y_value.Substring(0, y_value_trim);
                                Console.WriteLine(y_value);
                            }
                        }

                        Console.WriteLine(y_value);



                        //if this current y value is higher than whats stored we swap them
                        if (highest_y < Convert.ToDouble(y_value))
                            highest_y = Convert.ToDouble(y_value);
                    }
                }
            }
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Highest Y value = " + highest_y);
            Console.WriteLine("--------------------------------");



            //time to workout how much space we have left and what hat we can use

            double available_space = 0;
            available_space = Convert.ToDouble(y_dim) - highest_y;
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Available space: " + available_space.ToString());
            Console.WriteLine("--------------------------------");
            double hat_type = 0;
            switch (Convert.ToDouble(x_dim))
            {
                case 2200:
                    hat_type = 1900;
                    break;
                case 2500:
                    hat_type = 2200;
                    break;
                case 3000:
                    hat_type = 2500;
                    break;
                case 3100:
                    hat_type = 2800;
                    break;
            }

            //if the value is >= 240 then check which is best else use flathats
            string hat_string = "";
            if (available_space >= 240)
            {
                //check the stocks for which is needed most 
                 sql = "select type,stock from (SELECT case when  [description] like '%top%' then Left([description],8) + ' Hats' else Left([description],9) +' Hats' end as [type] ,sum(current_stock) as stock from dbo.bending_stock_items " +
                    "where top_hat = -1 or flat_hat = -1 and description not like '%sr2%' group by case when[description] like '%top%' then Left([description],8) +' Hats' else Left([description],9) +' Hats' end) a " +
                    "where type like '%" + hat_type.ToString() + "%'";

                int tophat_stock = 0;
                int flathat_stock = 0;

                using (SqlConnection conn = new SqlConnection(CONNECT.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        flathat_stock = Convert.ToInt32(dt.Rows[0][1].ToString());
                        tophat_stock = Convert.ToInt32(dt.Rows[1][1].ToString());

                        if (flathat_stock < tophat_stock)
                            hat_string = "Flat Hat";
                        else
                            hat_string = "Top Hat";

                    }
                    conn.Close();
                }

            }
            else if (available_space >= 125)
            {
                hat_string = "Flat Hat";
            }
            else
            {
                Environment.Exit(0); //cant fit any so its whatever
            }

            int quantity = 0;
            //workout how many possible hats we could fit on this sheet;
            if (hat_string == "Flat Hat")
                quantity = Convert.ToInt32(Math.Floor(available_space / 125));
            else
                quantity = Convert.ToInt32(Math.Floor(available_space / 240));

            Console.WriteLine("--------------------------------");
            Console.WriteLine(quantity + " x " + hat_type + " " + hat_string);
            Console.WriteLine("--------------------------------");


            //insert to batch_space_master

            sql = "INSERT INTO batch_space_master (group_id,batch_program,quantity,type,date_batched) VALUES (" + id + ",'" + program + "'," + quantity.ToString() + ",'" + hat_type + " " + hat_string + "',GETDATE())";

            using (SqlConnection conn = new SqlConnection(CONNECT.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
                    conn.Close();
            }

                Console.ReadLine();
        }
    }
}
