using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Mango.Framework.Module;
using Microsoft.AspNetCore.SignalR;
using Mango.Framework.Services;
using Mango.Framework.Services.RabbitMQ;
using Newtonsoft.Json;
namespace Mango.Module.Message
{
    public class ModuleInitializer:IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSignalR();
            //初始化连接对象池
            for (int i = 1; i <= 1000; i++)
            {
                SignalR.ConnectionManager.ConnectionUsers.Add(new SignalR.ConnectionUser()
                {
                    ConnectionIds = new List<string>(),
                    UserId = string.Empty
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseEndpoints(options=> {
                options.MapHub<SignalR.MessageHub>("/MessageHub");
            });
            //初始化消息队列信息
            var rabbitMQService = app.ApplicationServices.GetService<IRabbitMQService>();

            rabbitMQService.CreateQueue("message", false, false, false);
            rabbitMQService.CreateConsumeEvent("message", false, (obj, args) =>
            {
                try
                {
                    string msg = System.Text.Encoding.UTF8.GetString(args.Body);
                    string[] msgs = msg.Split('#');
                    if (msgs.Length == 2)
                    {
                        var hubContext =app.ApplicationServices.GetService<IHubContext<SignalR.MessageHub>>();
                        var connUser= SignalR.ConnectionManager.ConnectionUsers.Where(q => q.UserId == msgs[0]).FirstOrDefault();
                        if (connUser != null)
                        {
                            object[] _objData = new object[1];
                            var sendMsg = new SignalR.MessageData();
                            sendMsg.MessageBody = msgs[1];
                            sendMsg.MessageType = SignalR.MessageType.RespondNotice;
                            sendMsg.SendUserId = "0";
                            sendMsg.ReceveUserId = msgs[0];
                            _objData[0] = JsonConvert.SerializeObject(sendMsg);
                            foreach (var connId in connUser.ConnectionIds)
                            {
                                hubContext.Clients.Client(connId).SendCoreAsync("ReceiveMessage", _objData, CancellationToken.None);
                            }
                        }
                    }
                    //返回消息确认
                    rabbitMQService.BasicAck(args.DeliveryTag, true);
                }
                catch
                { }
            });
        }
    }
}
