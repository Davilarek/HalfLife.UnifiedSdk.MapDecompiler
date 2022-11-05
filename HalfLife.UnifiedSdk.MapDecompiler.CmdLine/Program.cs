﻿using HalfLife.UnifiedSdk.MapDecompiler.Decompilation;
using HalfLife.UnifiedSdk.MapDecompiler.Jobs;
using System.CommandLine;

namespace HalfLife.UnifiedSdk.MapDecompiler.CmdLine
{
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var filesOption = new Option<IEnumerable<FileInfo>>("--files", description: "List of files to decompile")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };

            var destinationOption = new Option<DirectoryInfo>(
                "--destination",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                description: "Directory to save decompiled maps");

            var mergeBrushesOption = new Option<bool>("--merge-brushes",
                getDefaultValue: () => true,
                description: "Whether to merge brushes");

            var includeLiquidsOption = new Option<bool>("--include-liquids",
                getDefaultValue: () => true,
                description: "Whether to include brushes with liquid content types");

            var brushOptimizationOption = new Option<BrushOptimization>("--brush-optimization",
                getDefaultValue: () => BrushOptimization.BestTextureMatch,
                description: "What to optimize brushes for");

            var rootCommand = new RootCommand("Half-Life Unified SDK Map Decompiler")
            {
                filesOption,
                destinationOption,
                mergeBrushesOption,
                includeLiquidsOption,
                brushOptimizationOption
            };

            rootCommand.SetHandler((files, destination, mergeBrushes, includeLiquids, brushOptimization) =>
            {
                MapDecompilerFrontEnd decompiler = new();
                DecompilerOptions decompilerOptions = new()
                {
                    MergeBrushes = mergeBrushes,
                    IncludeLiquids = includeLiquids,
                    BrushOptimization = brushOptimization
                };

                var destinationDirectory = destination.FullName;

                var jobs = files
                    .Select(f =>
                    {
                        var job = new MapDecompilerJob(f.FullName, destinationDirectory)
                        {
                            // TODO: figure out a way to make Output non-null all the time.
                            Output = string.Empty
                        };

                        job.MessageReceived += (job, message) => job.Output += message;

                        return job;
                    })
                    .ToList();

                Parallel.ForEach(jobs, job =>
                {
                    decompiler.Decompile(job, decompilerOptions, CancellationToken.None);

                    // Write completed log to console.
                    // Because we're decompiling multiple maps at the same time the log output would be mixed otherwise.
                    Console.WriteLine(job.Output);
                });
            }, filesOption, destinationOption, mergeBrushesOption, includeLiquidsOption, brushOptimizationOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}