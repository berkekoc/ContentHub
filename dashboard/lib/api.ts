// ContentHub API istemcisi. DTO'lar API'deki System.Text.Json (camelCase) çıktısıyla eşleşir.
// Enum'lar sayı olarak serileşir: ContentType 0=Video/1=Text, SortOption 0/1/2.

export const API_BASE =
  process.env.NEXT_PUBLIC_API_BASE_URL && process.env.NEXT_PUBLIC_API_BASE_URL.length > 0
    ? process.env.NEXT_PUBLIC_API_BASE_URL
    : "http://localhost:8080";

export enum ContentType {
  Video = 0,
  Text = 1,
}

export enum SortOption {
  Popularity = 0,
  Relevance = 1,
}

export interface ContentItemDto {
  id: string;
  title: string;
  description: string | null;
  type: ContentType;
  publishedAt: string;
  finalScore: number;
  relevance: number;
  providerCount: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface ScoreBreakdownDto {
  id: string;
  title: string;
  type: ContentType;
  baseScore: number;
  typeCoefficient: number;
  engagementScore: number;
  persistentScore: number;
  recencyPoints: number;
  finalScore: number;
  computedAt: string;
}

export interface SearchParams {
  keyword: string;
  type: ContentType | null;
  sort: SortOption;
  page: number;
  pageSize: number;
}

export function typeLabel(type: ContentType): string {
  return type === ContentType.Video ? "Video" : "Metin";
}

export function sortLabel(sort: SortOption): string {
  switch (sort) {
    case SortOption.Relevance:
      return "Alakalılık";
    default:
      return "Popülerlik";
  }
}

export async function search(params: SearchParams): Promise<PagedResult<ContentItemDto>> {
  const query = new URLSearchParams();
  if (params.keyword.trim().length > 0) query.set("keyword", params.keyword.trim());
  if (params.type !== null) query.set("type", String(params.type));
  query.set("sort", String(params.sort));
  query.set("page", String(params.page));
  query.set("pageSize", String(params.pageSize));

  const response = await fetch(`${API_BASE}/api/search?${query.toString()}`, {
    headers: { Accept: "application/json" },
  });
  if (!response.ok) {
    throw new Error(`Arama başarısız (HTTP ${response.status})`);
  }
  return (await response.json()) as PagedResult<ContentItemDto>;
}

export async function getScoreBreakdown(id: string): Promise<ScoreBreakdownDto> {
  const response = await fetch(`${API_BASE}/api/content/${id}/score`, {
    headers: { Accept: "application/json" },
  });
  if (!response.ok) {
    throw new Error(`Skor açıklaması alınamadı (HTTP ${response.status})`);
  }
  return (await response.json()) as ScoreBreakdownDto;
}

export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE}/health`, { cache: "no-store" });
    return response.ok;
  } catch {
    return false;
  }
}
