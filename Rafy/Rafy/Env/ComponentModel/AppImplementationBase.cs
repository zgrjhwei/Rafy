﻿/*******************************************************
 * 
 * 作者：胡庆访
 * 创建时间：20120310
 * 说明：此文件只包含一个类，具体内容见类型注释。
 * 运行环境：.NET 4.0
 * 版本号：1.0.0
 * 
 * 历史记录：
 * 创建文件 胡庆访 20120310
 * 编辑文件 崔化栋 20180502 14:00
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Rafy;
using Rafy.MetaModel;
using Rafy.MetaModel.View;
using Rafy.Utils;

namespace Rafy.ComponentModel
{
    /// <summary>
    /// 这个类为 ClientApp、ServerApp、WebApp 等类提供了一致的基类。
    /// </summary>
    public abstract class AppImplementationBase : IServerApp
    {
        /// <summary>
        /// 应用程序当前所处的阶段。
        /// </summary>
        public AppPhase Phase { get; private set; } = AppPhase.Created;

        /// <summary>
        /// 子类在合适的时间调用此方法来启动整个 Rafy 应用程序。
        /// 
        /// 注意，为了支持重新启动，这个类中的所有方法都应该可以运行多次。
        /// 
        /// 但是，第二次及之后的重启，不应该使用同一个 AppImplementationBase 的实例，否则可能会造成插件重复监听同一事件。
        /// </summary>
        protected void StartupApplication()
        {
            this.Phase = AppPhase.Starting;
            this.PrepareToStartup();

            this.InitEnvironment();
            RafyEnvironment.CreateStartupPlugins();

            //注册所有扩展属性
            //由于插件的 Intialize 方法中的应用层代码，有可能主动使用实体类而造成实体类的静态构造函数被执行，
            //所以必须在 Intialize 方法执行前，即任何实体被使用前，初始化所有的扩展属性。
            RafyEnvironment.InitExtensionProperties();

            //调用所有插件的 Initialize 方法。
            RafyEnvironment.InitializeStartupPlugins();
            this.OnStartupPluginsIntialized();

            //初始化启动期元数据
            this.CreateMeta();
            this.OnMetaCreating();

            this.OnMetaCreated();
            this.Phase = AppPhase.MetaCreated;

            //开始运行时行为。此行代码后的所有代码都可以看作运行时行为。
            this.OnRuntimeStarting();

            //启动主过程
            this.StartMainProcess();

            //整个初始化完毕。
            this.OnStartupCompleted();
            this.Phase = AppPhase.Running;
        }

        /// <summary>
        /// 此方法中会重置整个 Rafy 环境。这样可以保证各插件的注册机制能再次运行。
        /// 例如，当启动过程中出现异常时，可以重新使用 Startup 来启动应用程序开始全新的启动流程。
        /// </summary>
        protected virtual void PrepareToStartup()
        {
            RafyEnvironment.Reset();
            CommonModel.Instance.Reset();
            UIModel.Reset();
            WPFCommandNames.Clear();
        }

        /// <summary>
        /// 初始化应用程序的环境。
        /// 子类可在此方法中添加所需的插件、设置 <see cref="RafyEnvironment.Location"/> 等。
        /// </summary>
        protected virtual void InitEnvironment()
        {
            //如果配置了文化，则修改 UI 文化。否则使用系统默认的文化。
            var cultureName = RafyEnvironment.Configuration.Section.CurrentCulture;
            if (!string.IsNullOrWhiteSpace(cultureName))
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(cultureName);
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
                catch (CultureNotFoundException) { }
            }

            //如果是客户端，则所有线程使用一个身份（上下文）；
            if (RafyEnvironment.Location.IsWPFUI)
            {
                AppContext.SetProvider(new StaticAppContextProvider());
            }
#if NET45
            //如果是网站，则一个 HttpContext 使用一个身份（上下文）；否则，每个线程使用一个单独的身份（上下文）。
            else if (HttpContext.Current != null)
            {
                AppContext.SetProvider(new WebOrThreadAppContextProvider());
            }
#endif
            RafyEnvironment.InitCustomizationPath();

            RafyEnvironment.SetApp(this);

            //设置多国语言
            this.SetupLanguage();
        }

        /// <summary>
        /// 设置当前语言
        /// 
        /// 需要在所有 Translator 依赖注入完成后调用。
        /// </summary>
        private void SetupLanguage()
        {
            //当前线程的文化名，就是 Rafy 多国语言的标识。
            var culture = Thread.CurrentThread.CurrentUICulture.Name;
            if (!Translator.IsDevCulture(culture))
            {
                var translator = RafyEnvironment.Provider.Translator;
                //目前，在服务端进行翻译时，只支持一种语言。所以尽量在客户端进行翻译。
                translator.CurrentCulture = culture;
                translator.Enabled = true;
            }
        }

        /// <summary>
        /// 初始化必须在初始化期定义的各种元数据。
        /// </summary>
        protected virtual void CreateMeta() { }

        /// <summary>
        /// 子类重写此方法实现启动主逻辑。
        /// </summary>
        protected virtual void StartMainProcess() { }

        #region IServerApp 事件

        /// <summary>
        /// 所有实体元数据初始化完毕，包括实体元数据之间的关系。
        /// </summary>
        public event EventHandler StartupPluginsIntialized;

        /// <summary>
        /// 触发 StartupPluginsIntialized 事件。
        /// </summary>
        protected virtual void OnStartupPluginsIntialized()
        {
            var handler = this.StartupPluginsIntialized;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// 所有初始化期定义的元数据初始化完成时事件。
        /// </summary>
        public event EventHandler MetaCreating;

        /// <summary>
        /// 触发 MetaCreating 事件。
        /// </summary>
        protected virtual void OnMetaCreating()
        {
            var handler = this.MetaCreating;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// 所有初始化工作完成
        /// </summary>
        public event EventHandler MetaCreated;

        /// <summary>
        /// 触发 MetaCreated 事件。
        /// </summary>
        protected virtual void OnMetaCreated()
        {
            //冻结模块的元数据
            CommonModel.Modules.Freeze();

            var handler = this.MetaCreated;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// 应用程序运行时行为开始。
        /// </summary>
        public event EventHandler RuntimeStarting;

        /// <summary>
        /// 触发 RuntimeStarting 事件。
        /// </summary>
        protected virtual void OnRuntimeStarting()
        {
            var handler = this.RuntimeStarting;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// 应用程序完全退出
        /// </summary>
        public event EventHandler Exit;

        /// <summary>
        /// 触发 Exit 事件。
        /// </summary>
        protected virtual void OnExit()
        {
            var handler = this.Exit;
            if (handler != null) handler(this, EventArgs.Empty);

            this.Phase = AppPhase.Exited;
        }

        /// <summary>
        /// AppStartup 完毕
        /// </summary>
        public event EventHandler StartupCompleted;

        /// <summary>
        /// 触发 StartupCompleted 事件。
        /// </summary>
        protected virtual void OnStartupCompleted()
        {
            var handler = this.StartupCompleted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion
    }
}