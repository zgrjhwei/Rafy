﻿/*******************************************************
 * 
 * 作者：胡庆访
 * 创建时间：20120312
 * 说明：此文件只包含一个类，具体内容见类型注释。
 * 运行环境：.NET 4.0
 * 版本号：1.0.0
 * 
 * 历史记录：
 * 创建文件 胡庆访 20120312
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rafy;
using System.IO;
using Rafy.MetaModel.XmlConfig;

namespace Rafy.MetaModel.View
{
    /// <summary>
    /// Web 模型容器
    /// </summary>
    public class UIModel
    {
        private static XmlConfigManager _xmlConfigMgr;

        private static AggtBlocksRepository _aggtBlocks;

        private static EntityViewMetaFactory _views;

        private static WebCommandRepository _webCommands;

        private static WPFCommandRepository _wpfCommands;

        static UIModel()
        {
            _xmlConfigMgr = new XmlConfigManager();
            Reset();
        }

        /// <summary>
        /// xml 配置管理器
        /// </summary>
        public static XmlConfigManager XmlConfigMgr { get { return _xmlConfigMgr; } }

        public static AggtBlocksRepository AggtBlocks
        {
            get { return _aggtBlocks; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _aggtBlocks = value;
            }
        }

        public static EntityViewMetaFactory Views
        {
            get { return _views; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _views = value;
            }
        }

        public static WebCommandRepository WebCommands
        {
            get { return _webCommands; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _webCommands = value;
            }
        }

        public static WPFCommandRepository WPFCommands
        {
            get { return _wpfCommands; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _wpfCommands = value;
            }
        }

        public static void Freeze()
        {
            _webCommands.FreezeItems();
        }

        internal static void Reset()
        {
            _aggtBlocks = new AggtBlocksRepository(_xmlConfigMgr);
            _views = new EntityViewMetaFactory(_xmlConfigMgr);
            _webCommands = new WebCommandRepository();
            _wpfCommands = new WPFCommandRepository();
        }

        #region 初始化元数据

        /// <summary>
        /// 初始化程序中所有的实体DLL。
        /// 
        /// 此方法应该在程序初始化时调用。
        /// </summary>
        public static void InitCommandMetas()
        {
            if (RafyEnvironment.Location.IsWebUI)
            {
                //放在 Commands 下的文件夹会自动加入进来
                var cmdDir = ConfigurationHelper.GetAppSettingOrDefault("RafyCommandsDir", "Scripts/Commands/");
                var dir = RafyEnvironment.MapAbsolutePath(cmdDir);
                if (Directory.Exists(dir)) { _webCommands.AddByDirectory(dir); }

                //加入所有 Library 中 Commands 文件夹下的 js Resource。
                foreach (var plugin in RafyEnvironment.AllPlugins)
                {
                    _webCommands.AddByAssembly(plugin.Assembly);
                }
            }
            else
            {
                //加入所有 Module 中 Commands。
                foreach (var plugin in RafyEnvironment.AllPlugins)
                {
                    _wpfCommands.AddByAssembly(plugin.Assembly);
                }
            }

            RafyEnvironment.RuntimePluginLoaded += (o, e) =>
            {
                if (RafyEnvironment.Location.IsWebUI)
                {
                    _webCommands.AddByAssembly(e.Plugin.Assembly);
                }
                else
                {
                    _wpfCommands.AddByAssembly(e.Plugin.Assembly);
                }
            };
        }

        #endregion
    }
}