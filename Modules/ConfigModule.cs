using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Configuration;

namespace TurtleBot.Services
{
    public class ConfigModule
    {
        private readonly IConfiguration _config;

        private readonly Tuple<string, string>[] _configDefaults;

        private readonly Dictionary<string, WrapperService> references =
            new Dictionary<string, WrapperService>();

        private readonly Dictionary<string, string> _configNames = new Dictionary<string, string>();

        public string this[string key] => _config[key];

        public ConfigModule(IConfiguration config)
        {
            _configDefaults = config.GetChildren().Select(child => Tuple.Create(child.Key, child.Value)).ToArray();

            _config = config;
        }

        public void Enable(string key, string name) => _configNames.Add(key, name);

        public void AddBinding(WrapperService wraper, string key)
        {

            var configName = _configNames[key];

            if (configName.Equals(""))
            {
                throw new KeyNotFoundException();
            }
            
            references.Add(configName, wraper);
            
            wraper.SetValue(_configDefaults.First(def => def.Item1.Equals(configName)).Item2);
        }

        public void Execute(string change)
        {
            var query = change.Split(" ");

            var configName = _configNames[query[0]];
            
            if (query[1].Equals("reset"))
            {
                Reset(configName);
            }
            else
            {
                SetValue(configName, query[1]);
            }
        }

        public string GetValue(string key)
        {
            return _config[_configNames[key]];
        }

        private void SetValue(string key, string value)
        {

            foreach (var refrence in references.Where(refrence => refrence.Key.Equals(key)))
            {
                refrence.Value.SetValue(value);
            }
            
            _config[key] = value;
        }

        private void Reset(string key)
        {
            SetValue(key, _configDefaults.First(config => config.Item1.Equals(key)).Item2);
        }

        public void Reset()
        {
            foreach (var key in _configNames)
            {
                Reset(key.Value);
            }
        }
    }
}