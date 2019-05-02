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
    public class MySqlDbContainerFixture : DbContainerFixtureBase
    {
        protected override DockerRunCommandResult RunDbServerContainer()
        {
            var runDbContainerResult = _dockerCli.Run(
                Settings.MySqlDbImageName,
                environmentVariables: new List<EnvironmentVariable>
                {
                    new EnvironmentVariable("MYSQL_RANDOM_ROOT_PASSWORD", "yes"),
                    new EnvironmentVariable("MYSQL_DATABASE", Constants.DatabaseName),
                    new EnvironmentVariable("MYSQL_USER", Constants.DatabaseUserName),
                    new EnvironmentVariable("MYSQL_PASSWORD", Constants.DatabaseUserPwd),
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
            var retry = Policy.HandleResult(result: false).WaitAndRetry(retryCount: 36, i => TimeSpan.FromSeconds(5));
            return retry.Execute(() =>
            {
                try
                {
                    // Based on https://hub.docker.com/r/mysql/mysql-server/#starting-a-mysql-server-instance
                    string status = _dockerCli.GetContainerStatus(DbServerContainerName);
                    return status.Contains("healthy") && !status.Contains("starting");
                }
                catch
                {
                    // In case of any exception, we consider this retry as a failure and will retry again.
                    return false;
                }
            });
        }

        protected override void InsertSampleData()
        {
            const string sqlFile = "/tmp/mysql_setup.sql";
            var dbSetupScript = new ShellScriptBuilder()
                .CreateFile(sqlFile, GetSampleDataInsertionSql())
                // No space after the '-p' on purpose:
                // https://dev.mysql.com/doc/refman/5.7/en/connecting.html#option_general_password
                .AddCommand($"mysql -u {Constants.DatabaseUserName} -p{Constants.DatabaseUserPwd} < {sqlFile}")
                .ToString();

            var result = _dockerCli.Exec(DbServerContainerName, "/bin/sh", new[] { "-c", dbSetupScript });
            RunAsserts(() => Assert.True(result.IsSuccess), result.GetDebugInfo());
        }
    }
}
