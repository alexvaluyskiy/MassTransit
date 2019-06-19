// Copyright 2007-2018 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.ActiveMqTransport.Topology.Builders
{
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Entities;
    using Util;


    public class PublishEndpointBrokerTopologyBuilder :
        BrokerTopologyBuilder,
        IPublishEndpointBrokerTopologyBuilder
    {
        readonly PublishBrokerTopologyOptions _options;

        public PublishEndpointBrokerTopologyBuilder(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.MaintainHierarchy)
        {
            _options = options;
        }

        /// <summary>
        /// The exchange to which the published message is sent
        /// </summary>
        public TopicHandle Topic { get; set; }

        public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
        {
            if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
                return new ImplementedBuilder(this, _options);

            return this;
        }

        public BrokerTopology BuildBrokerTopology()
        {
            return new ActiveMqBrokerTopology(Exchanges, Queues, ConsumerBindings);
        }


        protected class ImplementedBuilder :
            IPublishEndpointBrokerTopologyBuilder
        {
            readonly IPublishEndpointBrokerTopologyBuilder _builder;
            readonly PublishBrokerTopologyOptions _options;
            TopicHandle _topic;

            public ImplementedBuilder(IPublishEndpointBrokerTopologyBuilder builder, PublishBrokerTopologyOptions options)
            {
                _builder = builder;
                _options = options;
            }

            public TopicHandle Topic
            {
                get => _topic;
                set
                {
                    _topic = value;
                    if (_builder.Topic != null)
                    {
                        var subscriptionName = string.Join("-", value.Topic.EntityName.Split('/').Reverse());
                        //var subscriptionDescription = new SubscriptionDescription(_builder.Topic.Topic.TopicDescription.Path,
                        //    GenerateSubscriptionName(subscriptionName))
                        //{
                        //    ForwardTo = value.Topic.TopicDescription.Path
                        //};

                        //_builder.CreateTopicSubscription(_builder.Topic, _topic, subscriptionDescription);

                        _builder.CreateQueue(GenerateSubscriptionName(subscriptionName), false, false);
                    }
                }
            }

            public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
            {
                if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
                    return new ImplementedBuilder(this, _options);

                return this;
            }

            public TopicHandle CreateTopic(string name, bool durable, bool autoDelete)
            {
                return _builder.CreateTopic(name, durable, autoDelete);
            }

            public QueueHandle CreateQueue(string name, bool durable, bool autoDelete)
            {
                return _builder.CreateQueue(name, durable, autoDelete);
            }

            public ConsumerHandle BindConsumer(TopicHandle topic, QueueHandle queue, string selector)
            {
                return _builder.BindConsumer(topic, queue, selector);
            }

            string GenerateSubscriptionName(string subscriptionName)
            {
                string name;
                if (subscriptionName.Length > 50)
                {
                    string hashed;
                    using (var hasher = new SHA1Managed())
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(subscriptionName);
                        byte[] hash = hasher.ComputeHash(buffer);
                        hashed = FormatUtil.Formatter.Format(hash).Substring(0, 6);
                    }

                    name = $"{subscriptionName.Substring(0, 43)}-{hashed}";
                }
                else
                    name = subscriptionName;

                return name;
            }
        }
    }
}
