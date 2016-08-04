namespace PageViewsLogDownloadService
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
            this.PageViewsLogDownloadServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.PageViewsLogDownloadServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // PageViewsLogDownloadServiceProcessInstaller
            // 
            this.PageViewsLogDownloadServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.PageViewsLogDownloadServiceProcessInstaller.Password = null;
            this.PageViewsLogDownloadServiceProcessInstaller.Username = null;
            // 
            // PageViewsLogDownloadServiceInstaller
            // 
            this.PageViewsLogDownloadServiceInstaller.ServiceName = "PageViewsLogDownloadService";
            this.PageViewsLogDownloadServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.PageViewsLogDownloadServiceProcessInstaller,
            this.PageViewsLogDownloadServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller PageViewsLogDownloadServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller PageViewsLogDownloadServiceInstaller;
    }
}