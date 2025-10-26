/**
 * Interface genérica para respostas da API
 * Segue o padrão utilizado pelo backend para respostas consistentes
 */
export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  code?: string;
  data?: T;
  errors?: string[];
}