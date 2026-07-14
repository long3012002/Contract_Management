import { useSourceProjectsQuery } from './useSourceProjectsQuery';
import { useSourceProjectsFilter } from './useSourceProjectsFilter';

export function useSourceProjects() {
  const query = useSourceProjectsQuery();
  const filter = useSourceProjectsFilter(query.allItems);

  return { query, filter };
}
