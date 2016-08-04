namespace GetServicePlanMetrics
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GetServicePlanMetricsProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.GetServicePlanMetricsInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // GetServicePlanMetricsProcessInstaller
            // 
            this.GetServicePlanMetricsProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.GetServicePlanMetricsProcessInstaller.Password = null;
            this.GetServicePlanMetricsProcessInstaller.Username = null;
            // 
            // GetServicePlanMetricsInstaller
            // 
            this.GetServicePlanMetricsInstaller.ServiceName = "GetServicePlanMetrics";
            this.GetServicePlanMetricsInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.GetServicePlanMetricsProcessInstaller,
            this.GetServicePlanMetricsInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller GetServicePlanMetricsProcessInstaller;
        private System.ServiceProcess.ServiceInstaller GetServicePlanMetricsInstaller;
    }
}