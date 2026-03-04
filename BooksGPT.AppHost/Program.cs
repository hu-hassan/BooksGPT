var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BooksGPT>("BooksGPT");

builder.Build().Run();
