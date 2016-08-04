namespace IISLogInsertService
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
            this.IISLogInsertServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.IISLogInsertServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // IISLogInsertServiceProcessInstaller
            // 
            this.IISLogInsertServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.IISLogInsertServiceProcessInstaller.Password = null;
            this.IISLogInsertServiceProcessInstaller.Username = null;
            // 
            // IISLogInsertServiceInstaller
            // 
            this.IISLogInsertServiceInstaller.ServiceName = "IISLogInsertService";
            this.IISLogInsertServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.IISLogInsertServiceProcessInstaller,
            this.IISLogInsertServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller IISLogInsertServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller IISLogInsertServiceInstaller;
    }
}