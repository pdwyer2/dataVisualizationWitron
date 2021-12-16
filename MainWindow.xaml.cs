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
    private bool partOfPackage;
    public Table(string name, List<string> col, List<string> linksForward, List<string> linksBackward)
    {
        Name = name;
        Col = new List<string>(col);
        LinksForward = new List<string>(linksForward);
        LinksBackward = new List<string>(linksBackward);
        partOfPackage = false;
    }
    public string Name { get; set; }

    public List<string> LinksForward { get; set; }

    public List<string> LinksBackward { get; set; }

    public List<string> Col { get; set; }

    public bool getPartOfPackage()
    {
        return partOfPackage;
    }

    /// <summary>
    /// This method is used to represent if this object is part of a package
    /// </summary>
    public void SetPackageFlag()
    {
        partOfPackage = partOfPackage == false;
    }

    public string ToString()
    {
        string result = "";
        int width = Name.Length+2;
        //finds the longest length in the table
        foreach(string s in Col)
        {
            if (s.Length + 2 > width)
                width = s.Length + 2;
        }
        //centers the table name on the dotted line
        int indent = width/2 - Name.Length/2; 
        for(int i = 0; i < indent; i++)
        {
            result = result + " ";
        }
        result = result + Name + "\n";
        //prints a dotted line
        for (int i = 0; i < width; i++)
        {
            result = result + "-";
        }
        result += "\n";
        foreach(string s in Col)
        {
            result = result + " " + s + " \n";
        }
        return result;
    }
}
namespace dataVisualizerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Dictionary<string, Table> masterList = new Dictionary<string, Table>();

        //THIS STRING IS ONLY GOOD FOR MATTHEWS'S PC: "TNS_ADMIN=E:\\School\\Current\\CAPSTONE\\WINDOWS.X64_193000_db_home\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=orclpdb;"
        string connectionString = "TNS_ADMIN=E:\\School\\Current\\CAPSTONE\\WINDOWS.X64_193000_db_home\\network\\admin;USER ID=OT;PASSWORD=Orcl1234;DATA SOURCE=orclpdb;";

        // this is the value to which for far the tables should link.  The dafault value is three.
        private int depth = 3;
        public MainWindow()
        {
            
        }

        /// <summary>
        /// This function populates the masterList dictionary with table objects.  
        /// It recursivly queries connections of each table depth conections away 
        /// from the original tableName parameter.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="d"></param>
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
            if (d > 0)
            {
                currentDepth--;
                foreach (string table in nextStep)
                {
                    createTableRelations(table, currentDepth);
                }
            }

        }

        /// <summary>
        /// This function creates a visualization of the relationship of tables in the form of a
        /// graph. Tables of a package are colored yellow while any others are of the default color.
        /// </summary>
        public static void Visualizer()
        {
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");
            
            foreach (KeyValuePair<string, Table> entry in masterList)
            {
                //This cleans up the masterList object to remove connections that will not be visualized
                for (int i = entry.Value.LinksForward.Count-1; i >= 0; --i)
                {
                    if (!masterList.ContainsKey(entry.Value.LinksForward[i]))
                        entry.Value.LinksForward.RemoveAt(i);
                }
                //This adds the nodes to the graph
                Microsoft.Msagl.Drawing.Node temp = new Microsoft.Msagl.Drawing.Node(entry.Key); // entry.Value.ToString() change this to the tostring when working
                temp.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Box;
                if (entry.Value.getPartOfPackage())
                    temp.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Yellow;
                graph.AddNode(temp);
            }
            //This adds edges to the graph
            foreach(KeyValuePair<string, Table> entry in masterList)
            {
                for (int i = entry.Value.LinksForward.Count - 1; i >= 0; --i)
                {
                    graph.AddEdge(entry.Key, entry.Value.LinksForward[i]);
                }
            }
            
            Microsoft.Msagl.WpfGraphControl.GraphViewer viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            //viewer.
            viewer.Graph = graph;

            //This is a console output in case the visualizer is not working
            foreach (KeyValuePair<string, Table> entry in masterList)
            {
                Console.WriteLine(entry.Value.ToString());
                if (entry.Value.LinksForward.Count != 0)
                {
                    Console.Write("Connections\n-----------\n");
                    foreach (string s in entry.Value.LinksForward)
                    {
                        Console.WriteLine(entry.Key + "  ---->  " + s);
                    }
                    Console.WriteLine("\n");
                }
                else
                    Console.WriteLine();
                Console.WriteLine();
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

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
                        "select tname from tab where tname = '" + name.ToUpper() + "'";
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
                        "SELECT Text " +
                        "FROM ALL_SOURCE " +
                        "WHERE name = '" + name.ToUpper() + "' " +
                        "and type = 'PACKAGE' " +
                        "ORDER BY line ";
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    string result = "";
                    while (dr.Read())
                    {
                        result += dr.GetString(0);
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

        /// <summary>
        /// This function scraps through a valid package and searches for valid tables referenced.
        /// </summary>
        /// <param name="packageName">The name of the package that is being scraped</param>
        /// <returns>A List<string> of valid tables the user has access to referenced in the package.</string></returns>
        private List<string> pakageParser(string packageName)
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
                        "WHERE name = '" + packageName + "' " +
                        "and type = 'PACKAGE' " +
                        "ORDER BY line ";
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    string package = "";
                    while(dr.Read())
                    {
                        package += dr.GetString(0);
                    }
                    // this is a temp string since I do not have access to the real package
                    package = "CREATE OR REPLACE PACKAGE BODY order_mgmt\n" +
                        "AS\n" +
                        "FUNCTION get_net_value(\n" +
                        "p_order_id NUMBER)\n" +
                        "RETURN NUMBER\n" +
                        "IS\n" +
                        "(This ( is ) a TEST()2)\n" +
                        "ln_net_value NUMBER\n" +
                        "BEGIN\n" +
                        "SELECT\n" +
                        "SUM(unit_price * quantity)\n" +
                        "INTO\n" +
                        "ln_net_value\n" +
                        "from\n" +
                        " order_items od, () test , regions\n" +
                        "where\n" +
                        "order_id = p_order_id;\n" +
                        "RETURN p_order_id;\n" +
                        "EXCEPTION\n" +
                        "no_data_found THEN\n" +
                        "DBMS_OUTPUT.PUT_LINE( SQLERRM );\n" +
                        "END get_net_value";

                    

                    //This removes all enters from the package to make it easier on the parser to differenciate``
                    package = package.Replace("\n", "");

                    //this removes all text in the package between and including ().
                    //This is done to prevent false scraping of a subquery

                    while (package.Contains("("))
                    {
                        int start = package.IndexOf("(");
                        int end = package.IndexOf(")");
                        string substring = package.Substring(start + 1, end - start);

                        if(substring.Contains("("))
                        {
                            int temp = substring.IndexOf("(");
                            package = package.Remove(end, 1);
                            package = package.Remove(temp + start + 1, 1);
                              
                        }
                        else
                        {
                            package = package.Remove(start, end - start + 1);
                        }
                    }
                    //This removes all text that is not between from and where reserved words
                    // the substring result should be the string that is parsed for table names.
                    string parse = "";
                    while (package.Contains("FROM") || package.Contains("from"))
                    {
                        int start = package.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase) + 4;
                        int end = package.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase);
                        parse = parse + package.Substring(start, end - start);
                        package = package.Remove(start - 4, end - start + 5);
                    }
                    dr.Close();


                    packageTables = parse.Split(',').ToList();
                    int countDown = packageTables.Count - 1;
                    while(countDown >= 0)
                    {
                        if (packageTables[countDown].Contains("join ", StringComparison.CurrentCultureIgnoreCase))
                            packageTables.RemoveAt(countDown);

                        else if (packageTables[countDown].Contains("using ", StringComparison.CurrentCultureIgnoreCase))
                            packageTables.RemoveAt(countDown);


                        // This should be a table reference and thus spaces are removed
                        else
                        {
                            // this splits the text by " " and checks if each variable is a valid namve
                            List<string> temp = packageTables[countDown].Split(" ").ToList();
                            for(int i = temp.Count -1; i>=0; i--)
                            {
                                if (TableExist(temp[i]))
                                {
                                    packageTables[countDown] = temp[i];
                                    break;
                                }
                                else
                                    temp.RemoveAt(i);
                            }
                            if (temp.Count == 0)
                                packageTables.RemoveAt(countDown);
                        
                            else
                                packageTables[countDown] = packageTables[countDown].ToUpper();
                            countDown--;
                        }
                    }

                }
                catch(Exception e)
                {

                }
            }
            return packageTables;
        }
        
        /// <summary>
        /// This function controls the search button tied to the table text field.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            TableSearch.IsEnabled = false;
            string tableText = tableSearchTextBox.Text.ToUpper();
            masterList.Clear();

            if (TableExist(tableText))
            {
                createTableRelations(tableText, depth);
                Visualizer();
            }
            else
            {
                _ = MessageBox.Show("Table '" + tableText + "' does not exist in the database", "Table Not Found");
            }

            TableSearch.IsEnabled = true;
        }

        /// <summary>
        /// This function contorls the depth of search combobox and the depth value tied to the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void depthOfSearch_Selection(object sender, SelectionChangedEventArgs e)
        {
            depth = Convert.ToInt32(depthOfSearch.SelectedIndex);
        }

        /// <summary>
        /// This function controls the search button tied the the package text field.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackageSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            PackageSearch.IsEnabled = false;

            string packageText = packageSearchTextBox.Text.ToUpper();
            masterList.Clear();

            if (PackageExist(packageText))
            {
                List<string> packageTables = new List<string>(pakageParser(packageText));
                if(packageTables == null)
                    MessageBox.Show("Table '" + packageText + "' does not contain any table references in the database", "Table Not Found");
                else
                {
                    foreach(string s in packageTables)
                    {
                        createTableRelations(s, depth);
                        masterList[s].SetPackageFlag();
                    }
                    Visualizer();
                }
            }
            else
            {
                _ = MessageBox.Show("Package '" + packageText + "' does not exist in the database", "Package Not Found");
            }

            PackageSearch.IsEnabled = true;
        }
    }
}
