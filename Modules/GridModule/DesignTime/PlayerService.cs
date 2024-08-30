using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportDJ.Modules.GridModule.DesignTime
{
    internal class PlayerService
        : IPlayerService
    {
        #region Public Properties

        public int Counter { get; set; }

        public IEnumerable<Entry> Entries { get; set; }

        public bool HasPaths { get; set; }

        public bool HasPlayeds { get; set; }

        public bool HasStack { get; set; }

        public bool HasUnplayeds { get; set; }

        public bool IsActive { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsEnding { get; set; }

        public bool IsForeground { get; set; }

        public bool IsLoop { get; set; }

        public bool IsPlaying { get; set; }

        public double? Progress { get; set; }

        public TimeSpan? Remaining { get; set; }

        public bool SavePosition { get; set; }

        public Track Track { get; set; }

        #endregion Public Properties

        #region Public Methods

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public void Initialize(Player player)
        {
            throw new NotImplementedException();
        }

        public void Next()
        {
            throw new NotImplementedException();
        }

        public void Previous()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Task SetFileAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetFolderAsync()
        {
            throw new NotImplementedException();
        }

        public Task ShuffleAsync()
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}