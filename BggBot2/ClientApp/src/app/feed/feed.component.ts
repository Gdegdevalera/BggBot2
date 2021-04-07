import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { FeedItem, FeedItemStatus } from '../app.models';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-feed',
  templateUrl: './feed.component.html',
  styleUrls: ['./feed.component.css']
})
export class FeedComponent implements OnInit, OnDestroy {

  public allStatuses = FeedItemStatus;
  public id: string;
  public items: FeedItem[];

  private sub: Subscription;

  constructor(private _activatedRoute: ActivatedRoute, private api: ApiService) { }

  ngOnInit() {
    this.sub = this._activatedRoute.params.subscribe(params => {
      this.id = params['subscriptionId'];
      this.api.getFeeds(this.id).subscribe(result => {
        this.items = result;
      }, error => console.error(error));
    });
  }

  onScroll() {
    const lastId = this.items[this.items.length - 1].id;
    this.api.getFeedsNextPage(this.id, lastId).subscribe(result => {
      this.items.push(...result);
    }, error => console.error(error));
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

}
