import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FeedItem, Subscription } from '../app.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  public getFeeds(subscriptionId: string): Observable<FeedItem[]> {
    return this.http.get<FeedItem[]>(this.baseUrl + 'subscriptions/' + subscriptionId);
  }

  public getFeedsNextPage(subscriptionId: string, lastId: string): Observable<FeedItem[]> {
    return this.http.get<FeedItem[]>(this.baseUrl + `subscriptions/${subscriptionId}?lastId=${lastId}`);
  }

  public getSubscriptions(): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(this.baseUrl + 'subscriptions');
  }

  public createSubscription(data): Observable<Subscription> {
    return this.http.post<Subscription>(this.baseUrl + 'subscriptions', data);
  }

  public startSubscription(id: string): Observable<Subscription> {
    return this.http.put<Subscription>(this.baseUrl + `subscriptions/${id}/start`, null);
  }

  public stopSubscription(id: string): Observable<Subscription> {
    return this.http.put<Subscription>(this.baseUrl + `subscriptions/${id}/stop`, null);
  }

  public testSubscription(data): Observable<FeedItem[]> {
    return this.http.post<FeedItem[]>(this.baseUrl + 'subscriptions/test', data);
  }
}
