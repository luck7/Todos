using System.Reflection;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Funq;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.Redis;

namespace Todos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost());

            app.Use(new RequestInfoHandler());
        }
    }

    // Define your ServiceStack web service request (i.e. Request DTO).
    [Route("/todos")]
    [Route("/todos/{Id}")]
    public class Todo
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public bool Done { get; set; }
    }

    // Create your ServiceStack rest-ful web service implementation. 
    public class TodoService : Service
    {
        public object Get(Todo todo)
        {
            //Return a single Todo if the id is provided.
            if (todo.Id != default(long))
                return Redis.As<Todo>().GetById(todo.Id);

            //Return all Todos items.
            return Redis.As<Todo>().GetAll();
        }

        // Handles creating and updating the Todo items.
        public Todo Post(Todo todo)
        {
            var redis = Redis.As<Todo>();

            //Get next id for new todo
            if (todo.Id == default(long))
                todo.Id = redis.GetNextSequence();

            redis.Store(todo);

            return todo;
        }

        // Handles creating and updating the Todo items.
        public Todo Put(Todo todo)
        {
            return Post(todo);
        }

        // Handles Deleting the Todo item
        public void Delete(Todo todo)
        {
            Redis.As<Todo>().DeleteById(todo.Id);
        }
    }

    // Create your ServiceStack web service application with a singleton AppHost.
    public class AppHost : AppHostBase
    {
        // Initializes your ServiceStack App Instance, with the specified assembly containing the services.
        public AppHost() : base("Backbone.js TODO", typeof(TodoService).GetTypeInfo().Assembly) { }

        // Configure the container with the necessary routes for your ServiceStack application.
        public override void Configure(Container container)
        {
            //Register Redis factory in Funq IoC. The default port for Redis is 6379.
            var redisHost = Environment.GetEnvironmentVariable("AWS_REDIS_HOST") ?? "localhost";
            container.Register<IRedisClientsManager>(new BasicRedisClientManager($"{redisHost}:6379"));
        }
    }
}
