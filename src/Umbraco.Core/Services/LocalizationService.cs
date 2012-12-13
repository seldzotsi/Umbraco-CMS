using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Auditing;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.Persistence.UnitOfWork;

namespace Umbraco.Core.Services
{
    /// <summary>
    /// Represents the Localization Service, which is an easy access to operations involving <see cref="Language"/> and <see cref="DictionaryItem"/>
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private static readonly Guid RootParentId = new Guid("41c7638d-f529-4bff-853e-59a0c2fb1bde");
        private readonly IUnitOfWork _unitOfWork;
	    private readonly IDictionaryRepository _dictionaryRepository;
	    private readonly ILanguageRepository _languageRepository;

        public LocalizationService() : this(new PetaPocoUnitOfWorkProvider())
        {
        }

        public LocalizationService(IUnitOfWorkProvider provider)
        {
            _unitOfWork = provider.GetUnitOfWork();
	        _dictionaryRepository = RepositoryResolver.Current.Factory.CreateDictionaryRepository(_unitOfWork);
	        _languageRepository = RepositoryResolver.Current.Factory.CreateLanguageRepository(_unitOfWork);
        }

        /// <summary>
        /// Gets a <see cref="IDictionaryItem"/> by its <see cref="Int32"/> id
        /// </summary>
        /// <param name="id">Id of the <see cref="IDictionaryItem"/></param>
        /// <returns><see cref="IDictionaryItem"/></returns>
        public IDictionaryItem GetDictionaryItemById(int id)
        {
            var repository = _dictionaryRepository;
            return repository.Get(id);
        }

        /// <summary>
        /// Gets a <see cref="IDictionaryItem"/> by its <see cref="Guid"/> id
        /// </summary>
        /// <param name="id">Id of the <see cref="IDictionaryItem"/></param>
        /// <returns><see cref="DictionaryItem"/></returns>
        public IDictionaryItem GetDictionaryItemById(Guid id)
        {
            var repository = _dictionaryRepository;

            var query = Query<IDictionaryItem>.Builder.Where(x => x.Key == id);
            var items = repository.GetByQuery(query);

            return items.FirstOrDefault();
        }

        /// <summary>
        /// Gets a <see cref="IDictionaryItem"/> by its key
        /// </summary>
        /// <param name="key">Key of the <see cref="IDictionaryItem"/></param>
        /// <returns><see cref="IDictionaryItem"/></returns>
        public IDictionaryItem GetDictionaryItemByKey(string key)
        {
            var repository = _dictionaryRepository;

            var query = Query<IDictionaryItem>.Builder.Where(x => x.ItemKey == key);
            var items = repository.GetByQuery(query);

            return items.FirstOrDefault();
        }

        /// <summary>
        /// Gets a list of children for a <see cref="IDictionaryItem"/>
        /// </summary>
        /// <param name="parentId">Id of the parent</param>
        /// <returns>An enumerable list of <see cref="IDictionaryItem"/> objects</returns>
        public IEnumerable<IDictionaryItem> GetDictionaryItemChildren(Guid parentId)
        {
            var repository = _dictionaryRepository;

            var query = Query<IDictionaryItem>.Builder.Where(x => x.ParentId == parentId);
            var items = repository.GetByQuery(query);

            return items;
        }

        /// <summary>
        /// Gets the root/top <see cref="IDictionaryItem"/> objects
        /// </summary>
        /// <returns>An enumerable list of <see cref="IDictionaryItem"/> objects</returns>
        public IEnumerable<IDictionaryItem> GetRootDictionaryItems()
        {
            var repository = _dictionaryRepository;

            var query = Query<IDictionaryItem>.Builder.Where(x => x.ParentId == RootParentId);
            var items = repository.GetByQuery(query);

            return items;
        }

        /// <summary>
        /// Checks if a <see cref="IDictionaryItem"/> with given key exists
        /// </summary>
        /// <param name="key">Key of the <see cref="IDictionaryItem"/></param>
        /// <returns>True if a <see cref="IDictionaryItem"/> exists, otherwise false</returns>
        public bool DictionaryItemExists(string key)
        {
            var repository = _dictionaryRepository;

            var query = Query<IDictionaryItem>.Builder.Where(x => x.ItemKey == key);
            var items = repository.GetByQuery(query);

            return items.Any();
        }

        /// <summary>
        /// Saves a <see cref="IDictionaryItem"/> object
        /// </summary>
        /// <param name="dictionaryItem"><see cref="IDictionaryItem"/> to save</param>
        /// <param name="userId">Optional id of the user saving the dictionary item</param>
        public void Save(IDictionaryItem dictionaryItem, int userId = -1)
        {
            var e = new SaveEventArgs();
            if (Saving != null)
                Saving(dictionaryItem, e);

            if (!e.Cancel)
            {
                _dictionaryRepository.AddOrUpdate(dictionaryItem);
                _unitOfWork.Commit();

                if (Saved != null)
                    Saved(dictionaryItem, e);

                Audit.Add(AuditTypes.Save, "Save DictionaryItem performed by user", userId == -1 ? 0 : userId,
                          dictionaryItem.Id);
            }
        }

        /// <summary>
        /// Deletes a <see cref="IDictionaryItem"/> object and its related translations
        /// as well as its children.
        /// </summary>
        /// <param name="dictionaryItem"><see cref="IDictionaryItem"/> to delete</param>
        /// <param name="userId">Optional id of the user deleting the dictionary item</param>
        public void Delete(IDictionaryItem dictionaryItem, int userId = -1)
        {
            var e = new DeleteEventArgs { Id = dictionaryItem.Id };
            if (Deleting != null)
                Deleting(dictionaryItem, e);

            if (!e.Cancel)
            {
                //NOTE: The recursive delete is done in the repository
                _dictionaryRepository.Delete(dictionaryItem);
                _unitOfWork.Commit();

                if (Deleted != null)
                    Deleted(dictionaryItem, e);

                Audit.Add(AuditTypes.Delete, "Delete DictionaryItem performed by user", userId == -1 ? 0 : userId, dictionaryItem.Id);
            }
        }

        /// <summary>
        /// Gets a <see cref="Language"/> by its id
        /// </summary>
        /// <param name="id">Id of the <see cref="Language"/></param>
        /// <returns><see cref="Language"/></returns>
        public ILanguage GetLanguageById(int id)
        {
            var repository = _languageRepository;
            return repository.Get(id);
        }

        /// <summary>
        /// Gets a <see cref="Language"/> by its culture code
        /// </summary>
        /// <param name="culture">Culture Code</param>
        /// <returns><see cref="Language"/></returns>
        public ILanguage GetLanguageByCultureCode(string culture)
        {
            var repository = _languageRepository;

            var query = Query<ILanguage>.Builder.Where(x => x.CultureName == culture);
            var items = repository.GetByQuery(query);

            return items.FirstOrDefault();
        }

        /// <summary>
        /// Gets all available languages
        /// </summary>
        /// <returns>An enumerable list of <see cref="ILanguage"/> objects</returns>
        public IEnumerable<ILanguage> GetAllLanguages()
        {
            var repository = _languageRepository;
            var languages = repository.GetAll();
            return languages;
        }

        /// <summary>
        /// Saves a <see cref="ILanguage"/> object
        /// </summary>
        /// <param name="language"><see cref="ILanguage"/> to save</param>
        /// <param name="userId">Optional id of the user saving the language</param>
        public void Save(ILanguage language, int userId = -1)
        {
            var e = new SaveEventArgs();
            if (Saving != null)
                Saving(language, e);

            if (!e.Cancel)
            {
                _languageRepository.AddOrUpdate(language);
                _unitOfWork.Commit();

                if (Saved != null)
                    Saved(language, e);

                Audit.Add(AuditTypes.Save, "Save Language performed by user", userId == -1 ? 0 : userId, language.Id);
            }
        }

        /// <summary>
        /// Deletes a <see cref="ILanguage"/> by removing it (but not its usages) from the db
        /// </summary>
        /// <param name="language"><see cref="ILanguage"/> to delete</param>
        /// <param name="userId">Optional id of the user deleting the language</param>
        public void Delete(ILanguage language, int userId = -1)
        {
            var e = new DeleteEventArgs { Id = language.Id };
            if (Deleting != null)
                Deleting(language, e);

            if (!e.Cancel)
            {
                //NOTE: There isn't any constraints in the db, so possible references aren't deleted
                _languageRepository.Delete(language);
                _unitOfWork.Commit();

                if (Deleted != null)
                    Deleted(language, e);

                Audit.Add(AuditTypes.Delete, "Delete Language performed by user", userId == -1 ? 0 : userId, language.Id);
            }
        }

        #region Event Handlers
        /// <summary>
        /// Occurs before Delete
        /// </summary>
        public static event EventHandler<DeleteEventArgs> Deleting;

        /// <summary>
        /// Occurs after Delete
        /// </summary>
        public static event EventHandler<DeleteEventArgs> Deleted;

        /// <summary>
        /// Occurs before Save
        /// </summary>
        public static event EventHandler<SaveEventArgs> Saving;

        /// <summary>
        /// Occurs after Save
        /// </summary>
        public static event EventHandler<SaveEventArgs> Saved;
        #endregion
    }
}