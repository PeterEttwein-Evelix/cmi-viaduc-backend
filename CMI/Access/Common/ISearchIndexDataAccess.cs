﻿using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Access.Common
{
    public interface ISearchIndexDataAccess
    {
        void UpdateDocument(ElasticArchiveRecord elasticArchiveRecord);
        void RemoveDocument(string archiveRecordId);

        ElasticArchiveRecord FindDocument(string archiveRecordId, bool includeFulltextContent);

        /// <summary>
        ///     Finds the document by its package identifier.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>ElasticArchiveRecord.</returns>
        ElasticArchiveRecord FindDocumentByPackageId(string packageId);

        /// <summary>
        ///     Gets the children to an archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <param name="allLevels">
        ///     if set to <c>true</c> all children are returned. If set to <c>false</c> only the direct
        ///     children are returned
        /// </param>
        /// <returns>IEnumerable&lt;ElasticArchiveRecord&gt;.</returns>
        IEnumerable<ElasticArchiveRecord> GetChildren(string archiveRecordId, bool allLevels);

        void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens);
    }
}