using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExternalActivatorService
{
 internal static class Program
 {
  /// <summary>
  /// The main entry point for the application.
  /// </summary>
  /// 
  private static string ConnectionString = "";

  private static EventLog eventLog;

  private static void Main(string[] args)
  {
   ServiceBase[] ServicesToRun;
   ServicesToRun = new ServiceBase[]
   {
    new ExternalActivator()
   };

   if (!Environment.UserInteractive)
    // running as service
   {
    ServiceBase.Run(ServicesToRun);
   }
   else
   {
    // running as console app
    Start(args);

    Stop();
   }
  }

  public static void Start(string[] args)
  {
   var connectionType = "";

   if (args.Length > 0) connectionType = args[0];

   if (connectionType == "-t") //Test server
    ConnectionString = ConfigurationManager.ConnectionStrings["TestConnection"].ConnectionString;
   else
    ConnectionString = ConfigurationManager.ConnectionStrings["ProductionConnection"].ConnectionString;

   eventLog = new EventLog("Application");
   eventLog.Source = "ExternalActivator";

   Console.WriteLine($"Waiting for message...");
   eventLog.WriteEntry("Waiting for message...", EventLogEntryType.Information, 2);

   while (true)
   {
    try
    {
     using (var connection = new SqlConnection(ConnectionString))
     {
      connection.Open();

      // Check for new messages
      using (var command =
             new SqlCommand(
              "SELECT TOP(1) ID, messagetext, toolname, params FROM dbo.MESSAGE WITH(NOLOCK) WHERE messagestatus=0",
              connection))
      {
       using (var reader = command.ExecuteReader())
       {
        if (reader.HasRows)
        {
         reader.Read();

         var messageid = reader.GetInt32(0);
         var message = reader.GetString(1);
         var toolname = reader.GetString(2);
         var qparams = reader.GetString(3);

         Console.WriteLine($"Message received: " + message);
         eventLog.WriteEntry($"Message received: " + message, EventLogEntryType.Information, 3);

         //set current directory for exe file
         Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

         // Process the message and insert response
         var responseid = 1;
         var filename = "";
         var path = Environment.CurrentDirectory;

         Console.WriteLine($"Executable name: " + toolname);
         eventLog.WriteEntry($"Executable name: " + toolname, EventLogEntryType.Information, 4);

         if (toolname == "regitra")
          filename = Path.Combine(path, "regitra.exe");
         else if (toolname == "vienna")
          filename = Path.Combine(path, "vienna.exe");
         else if (toolname == "vies")
          filename = Path.Combine(path, "vies.exe");
         else if (toolname == "eshop")
          filename = Path.Combine(path, "eshop.exe");
         else if (toolname == "navintegration") filename = Path.Combine(path, "b_navintegration.exe");

         var proc = Process.Start(filename, qparams);
         proc.WaitForExit();

         responseid = proc.ExitCode;
         proc.Close();

         Console.WriteLine($"Response: " + responseid.ToString());
         eventLog.WriteEntry($"Response: " + responseid.ToString(), EventLogEntryType.Information, 5);

         InsertResponse(messageid, responseid);

         Console.WriteLine($"Waiting for message...");
         eventLog.WriteEntry($"Waiting for message...", EventLogEntryType.Information, 6);
        }
       }
      }
     }
    }
    catch (Exception ex)
    {
     Console.WriteLine($"Error: {ex.Message}");
     eventLog.WriteEntry($"Error: {ex.Message}", EventLogEntryType.Error, 7);

     eventLog.Close();
    }

    // Add a delay
    Thread.Sleep(1000);
   }
  }

  public static void Stop()
  {
   eventLog.Close();
  }

  private static void InsertResponse(int messageid, int responseid)
  {
   using (var connection = new SqlConnection(ConnectionString))
   {
    connection.Open();

    // Insert the response into the Message table
    using (var command =
           new SqlCommand("UPDATE TOP(1) dbo.MESSAGE SET messagestatus=1, responseid=@responseid WHERE ID=@messageid",
            connection))
    {
     command.Parameters.AddWithValue("@responseid", responseid);
     command.Parameters.AddWithValue("@messageid", messageid);

     command.ExecuteNonQuery();
    }
   }
  }
 }
}