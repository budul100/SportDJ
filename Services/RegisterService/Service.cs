using Prism.Events;
using SportDJ.Commons.Events.Registers;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using System;
using System.Collections.Generic;

namespace SportDJ.Services.RegisterService
{
    public class Service
        : IRegisterService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly Dictionary<string, List<Register>> registers = new();

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Methods

        public int GetCount(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            Register(track);

            return registers[track.Path].Count;
        }

        public void Register(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (string.IsNullOrWhiteSpace(track.Path))
            {
                throw new ArgumentException(
                    message: "A track path is required.",
                    paramName: nameof(track));
            }

            if (!registers.ContainsKey(track.Path))
            {
                registers.Add(
                    key: track.Path,
                    value: new List<Register>());
            }
        }

        public void Start(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            Register(track);

            var register = new Register
            {
                Started = DateTime.Now
            };

            registers[track.Path].Add(register);

            eventAggregator.GetEvent<RegisterUpdatedEvent>()?.Publish();
        }

        #endregion Public Methods
    }
}