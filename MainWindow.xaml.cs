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
    private string name;
    private List<string> linksForward;
    private List<string> linksBackward;
    private List<string> col;
    public Table(string name, List<string> col, List<string> linksForward, List<string> linksBackward)
    {
        this.name = name;
        this.col = new List<string>(col);
        this.linksForward = new List<string>(linksForward);
        this.linksBackward = new List<string>(linksBackward);

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
        //THIS STRING IS ONLY GOOD FOR MATTHEWS'S PC: "TNS_ADMIN=E:\\School\\Current\\CAPSTONE\\WINDOWS.X64_193000_db_home\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=orclpdb;"
        String connectionString = "TNS_ADMIN=E:\\School\\Current\\CAPSTONE\\WINDOWS.X64_193000_db_home\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=orclpdb;";
        private int depth = 3;  // this is the value to which for far the tables should link.  The dafault value is three.
        public MainWindow()
        {
            
        }

        private void createTableRelations(string tableName, int d)
        {         
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
                    List<string> resultsForwardLink = new List<string>();
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
                    List<string> resultsBackwardsLink = new List<string>();
                    while (dr.Read())
                    {
                        resultsBackwardsLink.Add(dr.GetString(0));
                    }

                    // if the table has not been created then create it and add it to the master list
                    if(masterList.ContainsKey(tableName) == false)
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
                        List<string> colOfTable = new List<string>();
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

   

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            pakageParser("BSLN");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// This function opens a conenction to the database to determine if the string parameter is a valid table name.
        /// </summary>
        /// <param name="name">This is checked to see if this is a valid table name the user has access to.</param>
        /// <returns>true if name is a valid table in the database and false otherwise.</returns>
        private Boolean TableExist(string name)
        {
            bool flag = false;
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                try
                {
                    con.Open();
                    OracleCommand cmd = con.CreateCommand();
                    cmd.CommandText =
                        "select tname from tab where tname = '" + name + "'";
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    string result = "";
                    while (dr.Read())
                    {
                        result = dr.GetString(0);
                    }
                    dr.Close();
                    if (result != "")
                        flag = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return flag;
        }
        private Boolean PackageExist(string name)
        {
            bool flag = false;
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                try
                {
                    con.Open();
                    OracleCommand cmd = con.CreateCommand();
                    cmd.CommandText =
                        "select tname from tab where tname = '" + name + "'";
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    string result = "";
                    while (dr.Read())
                    {
                        result = dr.GetString(0);
                    }
                    dr.Close();
                    if (result != "")
                        flag = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return flag;
        }
        private void pakageParser(string packageName)
        {
            List<string> packageTables = new List<string>();
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                try
                {
                    con.Open();
                    OracleCommand cmd = con.CreateCommand();
                    cmd.CommandText =
                        "SELECT Text " +
                        "FROM ALL_SOURCE " +
                        "WHERE type = 'PACKAGE' " +
                        "and name = '" + packageName + "' " +
                        "ORDER BY line ";
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    string package = "";
                    while(dr.Read())
                    {
                        package += dr.GetString(0);
                    }
                    
                    //package = package.Replace("\n", "");
                    while (package.Contains("("))
                    {
                        int start = package.IndexOf("(");
                        int end = package.IndexOf(")") + 1;
                        package = package.Remove(start, end - start);
                    }
                    Console.WriteLine(package);
                    dr.Close();
                }
                catch(Exception e)
                {

                }
            }
        }
        
        private void TableSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            TableSearch.IsEnabled = false;
            string tableText = tableSearch.Text;
            masterList.Clear();

            if (TableExist(tableText))
                createTableRelations(tableText, depth);
            else
                MessageBox.Show("Table '" + tableText + "' does not exist in the database", "Table Not Found");
            TableSearch.IsEnabled = true;
        }

        private void depthOfSearch_Selection(object sender, SelectionChangedEventArgs e)
        {
            depth = Convert.ToInt32(depthOfSearch.SelectedIndex);
        }
    }
}
