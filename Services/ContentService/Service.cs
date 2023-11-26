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

        private Content content;
        private bool isCalledAlready;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Methods

        public Content GetContent()
        {
            var result = default(Content);

            if (!isCalledAlready)
            {
                result = content;
                isCalledAlready = true;
            }

            return result;
        }

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

            var group = new Group()
            {
                Name = "Player Group",
                Contents = new Content[] { player1, player2 },
            };

            content = group;
            isCalledAlready = false;

            eventAggregator.GetEvent<ContentLoadedEvent>().Publish();
        }

        #endregion Public Methods
    }
}