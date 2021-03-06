﻿using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace InfoBaseListService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new ServiceProcessInstaller();
            this.serviceInstaller1 = new ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.DisplayName = "Служба управления списком баз 1С";
            this.serviceInstaller1.ServiceName = "InfoBaseListService";
            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

        }

        #endregion

        private ServiceProcessInstaller serviceProcessInstaller1;
        private ServiceInstaller serviceInstaller1;
    }
}