using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MultiTypedClient.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IStackExchangeClient stackClient;
        private readonly IGitHubClient gitHubClient;

        public ValuesController(
        IStackExchangeClient stackClient,
        IGitHubClient gitHubClient)
        {
            this.stackClient = stackClient;
            this.gitHubClient = gitHubClient;
        }

        // GET api/values/stack
        [HttpGet]
        [Route("stack")]
        public async Task<string> Stack()
        {
            return await stackClient.GetJson();
        }

        // GET api/values/github
        [HttpGet]
        [Route("github")]
        public async Task<string> GitHub()
        {
            return await gitHubClient.GetJson();
        }
    }
}
