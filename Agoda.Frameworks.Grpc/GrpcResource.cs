using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    public class GrpcResource
    {
        public string Url { get; }

        public Channel Channel { get; }

        public GrpcResource(string url, Channel channel)
        {
            Url = url;
            Channel = channel;
        }

        public GrpcResource WithChannel(Channel channel)
        {
            return new GrpcResource(Url, channel);
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GrpcResource)
            {
                return Url == (obj as GrpcResource).Url;
            }
            else
            {
                return false;
            }
        }
    }
}
