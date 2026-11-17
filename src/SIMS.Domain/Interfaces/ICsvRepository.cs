namespace SIMS.Domain.Interfaces;

public interface ICsvRepository<T> where T : class
{
    List<T> GetAll();
    T? GetById(string id);
    void Add(T entity);
    void Update(T entity);
    bool Delete(string id);
    void Save();
}
