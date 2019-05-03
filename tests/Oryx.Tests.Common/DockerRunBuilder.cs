using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Oryx.Tests.Common;

namespace Oryx.Tests.Common
{
    public class DockerRunBuilder
    {
        private readonly DockerRunProperties _dockerRunProperties;

        private class DockerRunProperties
        {
            public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new List<EnvironmentVariable>();
            public List<DockerVolume> Volumes { get; set; } = new List<DockerVolume>();
            public bool RemoveContainer { get; set; }
            public int HostPort { get; set; }
            public int ContainerPort { get; set; }
            public string LinkToContainer { get; set; }
            public string Command { get; set; }
            public string[] CommandArgs { get; set; }
            public bool RunInBackground { get; set; }
        }

        public DockerRunBuilder()
        {
            _dockerRunProperties = new DockerRunProperties();
        }

        public DockerRunBuilder WithEnvironmentVariable(string key, string value)
        {
            _dockerRunProperties.EnvironmentVariables.Add(new EnvironmentVariable(key, value));
            return this;
        }

        public DockerRunBuilder WithVolume(DockerVolume volume)
        {
            _dockerRunProperties.Volumes.Add(volume);
            return this;
        }

        public DockerRunBuilder WithPortMapping(int hostPort, int containerPort)
        {
            _dockerRunProperties.HostPort = hostPort;
            _dockerRunProperties.ContainerPort = containerPort;
            return this;
        }

        public DockerRunBuilder RemoveContainerAfterRun()
        {
            _dockerRunProperties.RemoveContainer = true;
            return this;
        }

        public DockerRunBuilder RunInBackground()
        {
            _dockerRunProperties.RunInBackground = true;
            return this;
        }

        public DockerRunBuilder LinkToContainer(string link)
        {
            _dockerRunProperties.LinkToContainer = link;
            return this;
        }

        public DockerRunBuilder WithCommand(string command, string[] commandArgs)
        {
            _dockerRunProperties.Command = command;
            _dockerRunProperties.CommandArgs = commandArgs;
            return this;
        }

        public string Build()
        {
            return null;
        }
    }
}
