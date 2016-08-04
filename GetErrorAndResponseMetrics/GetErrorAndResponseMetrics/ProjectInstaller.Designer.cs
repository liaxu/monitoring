namespace GetErrorAndResponseMetrics
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
            this.GetErrorAndResponseMetricsProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.GetErrorAndResponseMetricsInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // GetErrorAndResponseMetricsProcessInstaller
            // 
            this.GetErrorAndResponseMetricsProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.GetErrorAndResponseMetricsProcessInstaller.Password = null;
            this.GetErrorAndResponseMetricsProcessInstaller.Username = null;
            // 
            // GetErrorAndResponseMetricsInstaller
            // 
            this.GetErrorAndResponseMetricsInstaller.ServiceName = "GetErrorAndResponseMetrics";
            this.GetErrorAndResponseMetricsInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.GetErrorAndResponseMetricsProcessInstaller,
            this.GetErrorAndResponseMetricsInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller GetErrorAndResponseMetricsProcessInstaller;
        private System.ServiceProcess.ServiceInstaller GetErrorAndResponseMetricsInstaller;
    }
}