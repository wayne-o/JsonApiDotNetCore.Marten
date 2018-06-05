using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JsonApiDotNetCore.Data;
using JsonApiDotNetCore.Extensions;
using JsonApiDotNetCore.Internal.Generics;
using JsonApiDotNetCore.Internal.Query;
using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Services;
using Marten;
using Marten.Linq;

namespace JsonApiDotNetCore.Marten
{
    public class MartenEntityRepository<TEntity, TId>
        : IEntityRepository<TEntity, TId>
        where TEntity : class, IIdentifiable<TId>
    {
        private readonly IDocumentSession _documenentSession;
        private readonly IJsonApiContext _jsonApiContext;
        private IQueryable<TEntity> Entities => _documenentSession.Query<TEntity>();
        private readonly IGenericProcessorFactory _genericProcessorFactory;

        public MartenEntityRepository(IDocumentSession documenentSession, IJsonApiContext jsonApiContext)
        {
            _documenentSession = documenentSession;
            _jsonApiContext = jsonApiContext;
            _genericProcessorFactory = _jsonApiContext.GenericProcessorFactory;
        }

        public async Task<int> CountAsync(IQueryable<TEntity> entities)
        {
            return await _documenentSession.Query<TEntity>().CountAsync();
        }

        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            _documenentSession.Store<TEntity>(entity);
            await _documenentSession.SaveChangesAsync();
            return entity;
        }

        public Task<bool> DeleteAsync(TId id)
        {
            _documenentSession.Delete(id);
            return Task.FromResult(true);
        }

        public IQueryable<TEntity> Filter(IQueryable<TEntity> entities, FilterQuery filterQuery)
        {
            return entities.Filter(_jsonApiContext, filterQuery);
        }

        public Task<TEntity> FirstOrDefaultAsync(IQueryable<TEntity> entities)
        {
            return entities.FirstOrDefaultAsync();
        }

        public IQueryable<TEntity> Get()
        {
            List<string> fields = _jsonApiContext.QuerySet?.Fields;
            if (fields?.Any() ?? false)
            {
                return this.Entities.Select(fields);
            }

            return this.Entities;
        }

        public Task<TEntity> GetAndIncludeAsync(TId id, string relationshipName)
        {
            // tried to do somthing like this but got stuck: https://stackoverflow.com/questions/17414332/reflection-to-call-generic-method-with-lambda-expression-parameter
            //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/how-to-use-expression-trees-to-build-dynamic-queries
            return this.GetAsync(id);
        }

        public async Task<TEntity> GetAsync(TId id)
        {
            return await _documenentSession.LoadAsync<TEntity>(id.ToString());
        }

        public IQueryable<TEntity> Include(IQueryable<TEntity> entities, string relationshipName)
        {
            // tried to do somthing like this but got stuck: https://stackoverflow.com/questions/17414332/reflection-to-call-generic-method-with-lambda-expression-parameter
            //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/how-to-use-expression-trees-to-build-dynamic-queries
            return entities;
        }

        public async Task<IEnumerable<TEntity>> PageAsync(IQueryable<TEntity> entities, int pageSize, int pageNumber)
        {
            return await entities.Skip(pageNumber * pageSize).Take(pageSize).ToListAsync();
        }

        public IQueryable<TEntity> Sort(IQueryable<TEntity> entities, List<SortQuery> sortQueries)
        {
            return entities.Sort(sortQueries);
        }

        public Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> entities)
        {
            return entities.ToListAsync();
        }

        public async Task<TEntity> UpdateAsync(TId id, TEntity entity)
        {
            var oldEntity = await GetAsync(id);

            if (oldEntity == null)
                return null;

            foreach (var attr in _jsonApiContext.AttributesToUpdate)
                attr.Key.SetValue(oldEntity, attr.Value);

            foreach (var relationship in _jsonApiContext.RelationshipsToUpdate)
                relationship.Key.SetValue(oldEntity, relationship.Value);

            await _documenentSession.SaveChangesAsync();

            return oldEntity;
        }

        public async Task UpdateRelationshipsAsync(object parent, RelationshipAttribute relationship, IEnumerable<string> relationshipIds)
        {
            var genericProcessor = _genericProcessorFactory.GetProcessor<IGenericProcessor>(typeof(GenericProcessor<>), relationship.Type);
            await genericProcessor.UpdateRelationshipsAsync(parent, relationship, relationshipIds);
        }
    }
}
