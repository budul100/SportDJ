using Prism.Events;
using SportDJ.Commons.Events;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models;

namespace SportDJ.Services.ContentService
{
    public class Service
        : IContentService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public Set Set { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load()
        {
            var player1 = new Player()
            {
                Name = "Player 1",
            };

            var player2 = new Player()
            {
                Name = "Player 2",
            };

            var player3 = new Player()
            {
                Name = "Player 3",
            };

            var player4 = new Player()
            {
                Name = "Player 4",
            };

            var group = new Group()
            {
                Name = "Player Group",
                Contents = new Content[] { player1, player2, player3, player4 },
            };

            Set = new Set
            {
                Contents = new Content[] { group },
            };

            eventAggregator.GetEvent<ContentLoadedEvent>().Publish();
        }

        #endregion Public Methods
    }
}