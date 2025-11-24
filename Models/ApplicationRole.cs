using Microsoft.AspNet.Identity;
using System;

namespace WebApp.Models
{
    public class ApplicationRole : IRole<string>
    {
        public ApplicationRole()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ApplicationRole(string name) : this()
        {
            Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
