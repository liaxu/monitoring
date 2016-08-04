namespace AILogInsertDBService
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
            this.AILogInsertDBServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.AILogInsertDBServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // AILogInsertDBServiceProcessInstaller
            // 
            this.AILogInsertDBServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.AILogInsertDBServiceProcessInstaller.Password = null;
            this.AILogInsertDBServiceProcessInstaller.Username = null;
            // 
            // AILogInsertDBServiceInstaller
            // 
            this.AILogInsertDBServiceInstaller.ServiceName = "AILogInsertDBService";
            this.AILogInsertDBServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.AILogInsertDBServiceProcessInstaller,
            this.AILogInsertDBServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller AILogInsertDBServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller AILogInsertDBServiceInstaller;
    }
}