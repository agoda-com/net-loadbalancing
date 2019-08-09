using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace Agoda.Frameworks.Grpc
{
    public class GrpcResource<TClient> where TClient : ClientBase<TClient>
    {
        public string Url { get; }

        public TClient Client { get; }

        public GrpcResource(string url, TClient client)
        {
            Url = url;
            Client = client;
        }

        public GrpcResource<TClient> WithClient(TClient client)
        {
            return new GrpcResource<TClient>(Url, client);
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GrpcResource<TClient>)
            {
                return Url == (obj as GrpcResource<TClient>).Url;
            }
            else
            {
                return false;
            }
        }
    }
}
