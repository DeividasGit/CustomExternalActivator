using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace ExternalActivatorService {
 public partial class ExternalActivator : ServiceBase {
  public ExternalActivator() {
   InitializeComponent();
  } 

  protected override void OnStart(string[] args) {
   Console.WriteLine($"OnStart Method running");

   using (EventLog eventLog = new EventLog("Application")) {
    eventLog.Source = "ExternalActivator";
    eventLog.WriteEntry("OnStart Method running", EventLogEntryType.Information, 1);
   }

   Task.Run(() => Program.Start(args));
  }

  protected override void OnStop() {
   Program.Stop();
  }

 }
}
