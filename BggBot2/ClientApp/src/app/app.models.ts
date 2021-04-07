export interface Subscription {
  id: string;
  telegramUserName: string;
  feedUrl: string;
  isEnabled: boolean;
  pendingCount: number;
  hasError: boolean;
}

export interface FeedItem {
  id: string;
  title: string;
  description: number;
  link: string;
  status: FeedItemStatus;
  publishDate: string;
  receivedDate: string;
  sentDate: string;
}

export enum FeedItemStatus {
  Unknown = 0,
  Pending = 1,
  Sent = 2,
  OnDemand = 3,
  Archived = 4
}
