using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Configuration;


public class Table
{
    public string Name
    { get; set; }
    public List<string> LinksForward
    { get; set; }
    public List<string> LinksBackward
    { get; set; }
    public List<string> Col
    { get; set; }
    public Table(string name, List<string> col, List<string> linksForward, List<string> linksBackward)
    {
        Name = name;
        Col = col;
        LinksForward = linksForward;
        LinksBackward = linksBackward;

    }
}

namespace dataVisualizerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, Table> masterList = new Dictionary<string, Table>();
        private int depth = 3;  // this is the value to which for far the tables should link.  The dafault value is three.
        OracleConnection con = null;
        public MainWindow()
        {
            //this.setConnection();
            //InitializeComponent();
        }

        private void createTableRelations(string tableName, int d)
        {
            String connectionString = "TNS_ADMIN=C:\\sqlInstaller\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=ORCLPDB;PERSIST SECURITY INFO=True";
            List<string> nextStep = new List<string>();
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                try
                {
                    con.Open();
                    OracleCommand cmd = con.CreateCommand();
                    // This query compares the primary key of the starting table to the foreign keys of all other tables and returns matches to find links.
                    cmd.CommandText =
                        "select  allCC.TABLE_NAME " +
                        "from all_constraints allC, all_tables allT, all_cons_columns allCC " +
                        "where   allT.TABLE_NAME = allC.TABLE_NAME " +
                        "and allT.TABLE_NAME = allCC.TABLE_NAME " +
                        "and allC.CONSTRAINT_NAME = allCC.CONSTRAINT_NAME " +
                        "and allT.owner = 'OT' " +
                        "and allT.STATUS = 'VALID' " +
                        "and allC.status = 'ENABLED' " +
                        "and allC.r_constraint_name = " +
                        "( " +
                        "select  allCC.constraint_name " +
                        "from    all_tables allT, all_constraints allC, all_cons_columns allCC " +
                        "where allT.TABLE_NAME = allC.TABLE_NAME " +
                        "and allT.TABLE_NAME = allCC.TABLE_NAME " +
                        "and allC.CONSTRAINT_NAME = allCC.CONSTRAINT_NAME " +
                        "and allT.TABLE_NAME = '" + tableName + "' " +
                        "and allT.owner = 'OT' " +
                        "and allT.STATUS = 'VALID' " +
                        "and allC.status = 'ENABLED' " +
                        "and allC.CONSTRAINT_TYPE in('P') " +
                        "and allCC.constraint_name like 'SYS%' " +
                        ")";

                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    List<String> resultsForwardLink = new List<String>();
                    while (dr.Read())
                    {
                        resultsForwardLink.Add(dr.GetString(0));
                    }

                    //This next query query compares all the foreign keys present in the starting table and to the primary keys of all other tables and returns matches to find links.
                    cmd.CommandText =
                        "select  allCC.TABLE_NAME " +
                        "from all_constraints allC, all_tables allT, all_cons_columns allCC " +
                        "where   allT.TABLE_NAME = allC.TABLE_NAME " +
                        "and allT.TABLE_NAME = allCC.TABLE_NAME " +
                        "and allC.CONSTRAINT_NAME = allCC.CONSTRAINT_NAME " +
                        "and allT.owner = 'OT' " +
                        "and allT.STATUS = 'VALID' " +
                        "and allC.status = 'ENABLED' " +
                        "and allC.constraint_name IN " +
                        "( " +
                        "select allC.r_constraint_name " +
                        "from    all_tables allT, all_constraints allC, all_cons_columns allCC " +
                        "where allT.TABLE_NAME = allC.TABLE_NAME " +
                        "and allT.TABLE_NAME = allCC.TABLE_NAME " +
                        "and allC.CONSTRAINT_NAME = allCC.CONSTRAINT_NAME " +
                        "and allT.TABLE_NAME = '" + tableName + "' " +
                        "and allT.owner = 'OT' " +
                        "and allT.STATUS = 'VALID' " +
                        "and allC.status = 'ENABLED' " +
                        "and allC.CONSTRAINT_TYPE in ('R') " +
                        "and allC.r_constraint_name like 'SYS%' " +
                        ")";

                    cmd.CommandType = CommandType.Text;
                    dr = cmd.ExecuteReader();
                    List<String> resultsBackwardsLink = new List<String>();
                    while (dr.Read())
                    {
                        resultsBackwardsLink.Add(dr.GetString(0));
                    }

                    // if the table has not been created then create it and add it to the master list
                    if (masterList.ContainsKey(tableName) == false)
                    {
                        cmd.CommandText =
                        "select col.column_name " +
                        "from sys.all_tab_columns col " +
                        "inner " +
                        "join sys.all_tables t on col.owner = t.owner " +
                        "and col.table_name = t.table_name " +
                        "where col.owner = 'OT' " +
                        "and col.table_name = '" + tableName + "' " +
                        "order by col.column_id ";

                        cmd.CommandType = CommandType.Text;
                        dr = cmd.ExecuteReader();
                        List<String> colOfTable = new List<String>();
                        while (dr.Read())
                        {
                            colOfTable.Add(dr.GetString(0));
                        }
                        masterList.Add(tableName, new Table(tableName, colOfTable, resultsForwardLink, resultsBackwardsLink));
                    }
                    dr.Close();
                    resultsForwardLink.AddRange(resultsBackwardsLink);
                    nextStep = new List<string>(resultsForwardLink);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            int currentDepth = d;
            if (d == 0)
            {
                Console.WriteLine("Exit");
            }
            else
            {
                currentDepth--;
                //List<string> nextStep = new List<string>(resultsForwardLink.AddRange(resultsBackwardsLink));
                foreach (string table in nextStep)
                {
                    createTableRelations(table, currentDepth);
                }
            }
        }
        //private void setConnection()
        //{
        //    String connectionString = ConfigurationManager.ConnectionStrings["sampleDB"].ConnectionString;
        //    con = new OracleConnection(connectionString);
        //}

        private void displayConnections()
        {
            foreach (KeyValuePair<string, Table> item in masterList)
            {
                Console.WriteLine(item.Value.Name);
                Console.WriteLine("---------------------");
                foreach (string i in item.Value.Col)
                {
                    Console.WriteLine(i);
                }
                Console.WriteLine("---------------------");
                Console.WriteLine("\n");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.createTableRelations("ORDERS", depth);
            Console.WriteLine();
            //this.displayConnections();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //con.Close();
        }

        private void tableSearchButton_Click(object sender, RoutedEventArgs e)
        {
            string tableText = tableSearch.Text;
            createTableRelations(tableText, depth);
            displayConnections();
            setTableData(masterList);
        }

        private void setTableData(Dictionary<string,Table> dictionary)
        {
            foreach (KeyValuePair<string, Table> item in masterList)
            {
                string tableName = item.Key;
                String connectionString = "TNS_ADMIN=C:\\sqlInstaller\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=ORCLPDB;PERSIST SECURITY INFO=True";
                using (OracleConnection con = new OracleConnection(connectionString))
                {
                    con.Open();
                    OracleCommand cmd = con.CreateCommand();
                    cmd.CommandText =
                               "select col.column_name as \"" + tableName + "\" " +
                               "from sys.all_tab_columns col " +
                               "inner " +
                               "join sys.all_tables t on col.owner = t.owner " +
                               "and col.table_name = t.table_name " +
                               "where col.owner = 'OT' " +
                               "and col.table_name = '" + tableName + "' " +
                               "order by col.column_id ";

                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    var dataTable = new DataTable();
                    dataTable.Load(dr);
                    var dataGrid = new DataGrid(); //{ ItemsSource = dataTable.DefaultView, Columns=item.Key };
                    dataGrid.ItemsSource = dataTable.DefaultView;
                    //dataGrid.MaxColumnWidt = '*';

                    //DataGridContainer.Children.Add(dataGrid);
                    
                }
            }


            /*
                List<string> tableData = new List<string>();
            tableData.Add(t.Name);
            foreach(string s in t.Col)
            {
                tableData.Add(s);
            }

            foreach(string s in tableData)
            {
                Console.WriteLine(s);
            }
            */

            //var dataGrid = new DataGrid { ItemsSource = tableData };
            //DataGridContainer.Items.Add(dataGrid);
            //string tableName = t.Name;
            /*
            String connectionString = "TNS_ADMIN=C:\\sqlInstaller\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=ORCLPDB;PERSIST SECURITY INFO=True";
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                OracleCommand cmd = con.CreateCommand();
                cmd.CommandText =
                           "select col.column_name as \"" + tableName + "\" " +
                           "from sys.all_tab_columns col " +
                           "inner " +
                           "join sys.all_tables t on col.owner = t.owner " +
                           "and col.table_name = t.table_name " +
                           "where col.owner = 'OT' " +
                           "and col.table_name = '" + tableName + "' " +
                           "order by col.column_id ";

                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load(dr);
                var dataGrid = new DataGrid { ItemsSource = dataTable.DefaultView, Width='*'};
                DataGridContainer.Items.Add(dataGrid);
            }
            */
            
        }

    }
}