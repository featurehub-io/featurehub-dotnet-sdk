

using System.Collections.Generic;
using ToDoAspCore.Models;

namespace ToDoAspCore.Services
{
    public interface ITodoServiceRepository
    {
        List<Todo> UsersTodos(string user);
    }

    public class TodoServiceInMemoryRepository : ITodoServiceRepository
    {
        private Dictionary<string, List<Todo>> todos = new Dictionary<string, List<Todo>>();
        
        public List<Todo> UsersTodos(string user)
        {
            if (this.todos.ContainsKey(user))
            {
                return this.todos[user];
            }

            todos[user] = new List<Todo>();
            
            return todos[user];
        }
    }
}