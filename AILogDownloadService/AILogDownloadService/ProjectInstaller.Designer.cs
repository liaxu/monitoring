namespace AILogDownloadService
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
            this.AILogDownloadServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.AILogDownloadServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // AILogDownloadServiceProcessInstaller
            // 
            this.AILogDownloadServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.AILogDownloadServiceProcessInstaller.Password = null;
            this.AILogDownloadServiceProcessInstaller.Username = null;
            // 
            // AILogDownloadServiceInstaller
            // 
            this.AILogDownloadServiceInstaller.ServiceName = "AILogDownloadService";
            this.AILogDownloadServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.AILogDownloadServiceProcessInstaller,
            this.AILogDownloadServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller AILogDownloadServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller AILogDownloadServiceInstaller;
    }
}