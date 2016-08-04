namespace IISLogDownloadService
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
            this.IISLogDownloadServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.IISLogDownloadServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // IISLogDownloadServiceProcessInstaller
            // 
            this.IISLogDownloadServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.IISLogDownloadServiceProcessInstaller.Password = null;
            this.IISLogDownloadServiceProcessInstaller.Username = null;
            // 
            // IISLogDownloadServiceInstaller
            // 
            this.IISLogDownloadServiceInstaller.ServiceName = "IISLogDownloadService";
            this.IISLogDownloadServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.IISLogDownloadServiceProcessInstaller,
            this.IISLogDownloadServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller IISLogDownloadServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller IISLogDownloadServiceInstaller;
    }
}