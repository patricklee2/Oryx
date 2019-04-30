﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Oryx.Tests.Common;
using Polly;
using Xunit;

namespace Microsoft.Oryx.Integration.Tests.LocalDockerTests.Fixtures
{
    public class PostgreSqlDbContainerFixture : DbContainerFixtureBase
    {
        protected override DockerRunCommandResult RunDbServerContainer()
        {
            var runDbContainerResult = _dockerCli.Run(
                    Settings.PostgresDbImageName,
                    environmentVariables: new List<EnvironmentVariable>
                    {
                        new EnvironmentVariable("POSTGRES_DB", Constants.DatabaseName),
                        new EnvironmentVariable("POSTGRES_USER", Constants.DatabaseUserName),
                        new EnvironmentVariable("POSTGRES_PASSWORD", Constants.DatabaseUserPwd),
                    },
                    volumes: null,
                    portMapping: null,
                    link: null,
                    runContainerInBackground: true,
                    command: null,
                    commandArguments: null);

            RunAsserts(() => Assert.True(runDbContainerResult.IsSuccess), runDbContainerResult.GetDebugInfo());
            return runDbContainerResult;
        }

        protected override bool WaitUntilDbServerIsUp()
        {
            // Try 33 times at most, with a constant 3s in between attempts
            var retry = Policy.HandleResult(result: false).WaitAndRetry(33, i => TimeSpan.FromSeconds(3));
            return retry.Execute(() =>
            {
                try
                {
                    return _dockerCli.GetContainerLogs(DbServerContainerName)
                    .Contains("database system is ready to accept connections");
                }
                catch
                {
                    return false;
                }
            });
        }

        protected override void InsertSampleData()
        {
            const string sqlFile = "/tmp/setup.sql";
            var dbSetupScript = new ShellScriptBuilder()
                .CreateFile(sqlFile, GetSampleDataInsertionSql())
                .AddCommand($"PGPASSWORD={Constants.DatabaseUserPwd} psql -h localhost -d {Constants.DatabaseName} -U{Constants.DatabaseUserName} < {sqlFile}")
                .ToString();

            var result = _dockerCli.Exec(DbServerContainerName, "/bin/sh", new[] { "-c", dbSetupScript });
            RunAsserts(() => Assert.True(result.IsSuccess), result.GetDebugInfo());
        }
    }
}
